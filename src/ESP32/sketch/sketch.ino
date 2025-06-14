// ADAPTADO: ESP32 envia e reproduz áudio do servidor usando PAM8406 no GPIO27
#include <WiFi.h>
#include <driver/i2s.h>
#include "AudioFileSourceHTTPStream.h"
#include "AudioGeneratorMP3.h"
#include "AudioOutputI2S.h"
#include "AudioFileSourceBuffer.h"

// ======= CONFIG WI-FI =======
const char *ssid = "casavillage";
const char *password = "familialinguini";

// ======= SERVIDOR .NET =======
const char *serverUrl = "http://192.168.0.101:5179/melissa/AskMelissaAudio";

// ======= PINOS I2S DO MICROFONE =======
#define I2S_WS 25
#define I2S_SD 35
#define I2S_SCK 32
#define I2S_PORT I2S_NUM_0
#define SAMPLE_RATE 16000
#define BITS_PER_SAMPLE I2S_BITS_PER_SAMPLE_16BIT
#define DMA_BUF_COUNT 8
#define DMA_BUF_LEN 1024

// ======= PINO DE AUDIO PARA SAÍDA ANALÓGICA (PAM8406) =======
#define AUDIO_OUT_PIN 26  // GPIO26 conectado ao LIN do PAM8406

void setupI2S() {
  static bool initialized = false;
  if (initialized) return;
  initialized = true;

  const i2s_config_t i2s_config = {
    .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_TX),  // RX e TX ativados
    .sample_rate = SAMPLE_RATE,
    .bits_per_sample = BITS_PER_SAMPLE,
    .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT,
    .communication_format = I2S_COMM_FORMAT_I2S,
    .intr_alloc_flags = 0,
    .dma_buf_count = DMA_BUF_COUNT,
    .dma_buf_len = DMA_BUF_LEN,
    .use_apll = false,
    .tx_desc_auto_clear = false,
    .fixed_mclk = 0
  };

  const i2s_pin_config_t pin_config = {
    .bck_io_num = I2S_SCK,
    .ws_io_num = I2S_WS,
    .data_out_num = AUDIO_OUT_PIN,  // Saída para o amplificador
    .data_in_num = I2S_SD
  };

  i2s_driver_install(I2S_PORT, &i2s_config, 0, NULL);
  i2s_set_pin(I2S_PORT, &pin_config);
  i2s_zero_dma_buffer(I2S_PORT);
}

void setup() {
  Serial.begin(115200);
  WiFi.begin(ssid, password);
  Serial.print("Conectando ao Wi-Fi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWi-Fi conectado!");
  setupI2S();
  Serial.println("I2S do microfone iniciado!");
}

void sendAudioRawAndPlay() {
  WiFiClient client;
  if (!client.connect("192.168.0.6", 5179)) {
    Serial.println("Falha ao conectar ao servidor");
    return;
  }

  // Envia áudio RAW
  client.println("POST /melissa/AskMelissaAudio HTTP/1.1");
  client.println("Host: 192.168.0.101");
  client.println("Transfer-Encoding: chunked");
  client.println("Content-Type: application/octet-stream");
  client.println("Connection: close");
  client.println();

  const int numChunks = 100;
  for (int i = 0; i < numChunks; i++) {
    uint8_t buffer[1024];
    size_t bytesRead;
    i2s_read(I2S_PORT, buffer, sizeof(buffer), &bytesRead, portMAX_DELAY);
    client.printf("%X\r\n", bytesRead);
    client.write(buffer, bytesRead);
    client.print("\r\n");
    Serial.print(".");
  }
  client.print("0\r\n\r\n");
  Serial.println("\nÁudio enviado com sucesso! Aguardando resposta...");

  delay(10000);  // tempo para o servidor responder

  // Reproduz MP3 vindo do servidor
  AudioFileSourceHTTPStream *file_http = new AudioFileSourceHTTPStream(serverUrl);
  AudioFileSourceBuffer *file = new AudioFileSourceBuffer(file_http, 2048);  // buffer
  AudioGeneratorMP3 *mp3 = new AudioGeneratorMP3();
  AudioOutputI2S *out = new AudioOutputI2S();
  out->SetOutputModeMono(true);
  out->SetPinout(22, 21, -1);
  out->SetGain(0.9);

  if (mp3->begin(file, out)) {
    while (mp3->isRunning()) {
      if (!mp3->loop()) break;
    }
    mp3->stop();
    Serial.println("Reprodução finalizada.");
  } else {
    Serial.println("Erro ao iniciar decodificação MP3.");
  }
  Serial.println("Erro ao iniciar decodificação MP3.");
  
  client.stop();
}


void loop() {
  Serial.println("Capturando e enviando áudio...");
  sendAudioRawAndPlay();
  delay(15000);
}
