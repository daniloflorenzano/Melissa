#include <WiFi.h>
#include <driver/i2s.h>
#include <driver/dac.h>
#include "AudioFileSource.h"
#include "AudioGeneratorMP3.h"
#include "AudioOutputI2S.h"

// Classe para playback por streaming direto do WiFiClient
class AudioFileSourceFromClient : public AudioFileSource {
public:
  WiFiClient *client;
  bool firstHeader;
  AudioFileSourceFromClient(WiFiClient *c)
    : client(c), firstHeader(true) {}

  uint32_t read(void *data, uint32_t len) override {
    if (!client->connected()) return 0;

    int avail = client->available();
    if (avail <= 0) return 0;

    uint32_t toRead = (uint32_t)avail;
    if (toRead > len) toRead = len;

    int r = client->read((uint8_t *)data, toRead);
    if (r > 0 && firstHeader) {
      // Ativar speaker no primeiro bloco de MP3
      firstHeader = false;
    }

    return (uint32_t)r;
  }

  bool isOpen() override {
    return client->connected();
  }

  bool close() override {
    client->stop();
    return true;
  }

  int available() {
    return client->available();
  }
};

// ------- Configurações Wi-Fi -------
#define SSID "..."
#define PASSWORD "..."

// ------- Servidor -------
#define SERVER_HOST "192.168.0.6"
#define SERVER_PORT 5179
#define SERVER_PATH "/melissa/AskMelissaAudio"

// ------- I2S (Microfone) -------
#define I2S_MIC_PORT I2S_NUM_1
#define I2S_SCK_PIN 32  // BCLK
#define I2S_WS_PIN 26   // LRCLK
#define I2S_SD_PIN 35   // DATA_IN

#define SAMPLE_RATE 16000
#define BITS_PER_SAMPLE I2S_BITS_PER_SAMPLE_16BIT
#define CHANNEL_FORMAT I2S_CHANNEL_FMT_ONLY_LEFT
#define COMM_FORMAT ((i2s_comm_format_t)(I2S_COMM_FORMAT_I2S | I2S_COMM_FORMAT_I2S_MSB))
#define DMA_BUF_COUNT 8
#define DMA_BUF_LEN 1024

// ------- Speaker Enable e DAC -------
#define SPEAKER_ENABLE_PIN 27
#define SPEAKER_GAIN 0.8f

// Timeout para início de MP3 (ms)
#define RESPONSE_TIMEOUT_MS 20000

uint8_t i2sBuffer[DMA_BUF_LEN];

void setupI2SMic() {
  i2s_config_t cfg = {
    .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX),
    .sample_rate = SAMPLE_RATE,
    .bits_per_sample = BITS_PER_SAMPLE,
    .channel_format = CHANNEL_FORMAT,
    .communication_format = COMM_FORMAT,
    .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,
    .dma_buf_count = DMA_BUF_COUNT,
    .dma_buf_len = DMA_BUF_LEN,
    .use_apll = false,
    .tx_desc_auto_clear = false,
    .fixed_mclk = 0
  };
  i2s_driver_install(I2S_MIC_PORT, &cfg, 0, NULL);
  i2s_pin_config_t pins = { .bck_io_num = I2S_SCK_PIN, .ws_io_num = I2S_WS_PIN, .data_out_num = I2S_PIN_NO_CHANGE, .data_in_num = I2S_SD_PIN };
  i2s_set_pin(I2S_MIC_PORT, &pins);
  i2s_zero_dma_buffer(I2S_MIC_PORT);
  i2s_start(I2S_MIC_PORT);
}

void setupDAC() {
  // Ativa canal DAC GPIO25
  dac_output_enable(DAC_CHANNEL_1);
}

void sendAudioAndStream() {
  Serial.println("=== Gravando e enviando áudio de voz ===");
  WiFiClient client;
  if (!client.connect(SERVER_HOST, SERVER_PORT)) {
    Serial.println("Erro: não conectou ao servidor");
    return;
  }
  // Envia chunks RAW
  client.printf("POST %s HTTP/1.1\r\n"
                "Host: %s\r\n"
                "Transfer-Encoding: chunked\r\n"
                "Content-Type: application/octet-stream\r\n"
                "Connection: close\r\n\r\n",
                SERVER_PATH, SERVER_HOST);
  for (int i = 0; i < 100; ++i) {
    size_t bytesRead = 0;
    i2s_read(I2S_MIC_PORT, i2sBuffer, DMA_BUF_LEN, &bytesRead, portMAX_DELAY);
    if (bytesRead == 0) continue;
    client.printf("%X\r\n", (unsigned int)bytesRead);
    client.write(i2sBuffer, bytesRead);
    client.print("\r\n");
  }
  client.print("0\r\n\r\n");
  client.flush();

  // Descarta headers
  while (client.connected()) {
    String line = client.readStringUntil('\n');
    if (line.length() <= 1) break;
  }

  // Aguarda início do fluxo MP3
  unsigned long start = millis();
  while (!client.available()) {
    if (millis() - start > RESPONSE_TIMEOUT_MS) {
      Serial.println("Timeout aguardando MP3");
      client.stop();
      return;
    }
    delay(10);
  }

  // Streaming MP3 diretamente
  Serial.println("=== Iniciando reprodução por streaming ===");
  digitalWrite(SPEAKER_ENABLE_PIN, HIGH);
  AudioFileSourceFromClient source(&client);
  AudioGeneratorMP3 mp3;
  AudioOutputI2S out(I2S_NUM_0, true);
  out.SetOutputModeMono(true);
  out.SetGain(SPEAKER_GAIN);

  if (!mp3.begin(&source, &out)) {
    Serial.println("Erro no decoder");
    digitalWrite(SPEAKER_ENABLE_PIN, LOW);
    client.stop();
    return;
  }

  while (mp3.isRunning()) {
    if (!mp3.loop()) {
      Serial.println("\n=== Reprodução finalizada ===");
      break;
    }
  }
  mp3.stop();

  digitalWrite(SPEAKER_ENABLE_PIN, LOW);
  Serial.println("Pronto para próxima gravação");
}

void setup() {
  Serial.begin(115200);
  WiFi.begin(SSID, PASSWORD);
  Serial.print("Conectando Wi-Fi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print('.');
  }
  Serial.println(" ok");
  pinMode(SPEAKER_ENABLE_PIN, OUTPUT);
  setupI2SMic();
  setupDAC();
}

void loop() {
  sendAudioAndStream();
  delay(15000);
}
