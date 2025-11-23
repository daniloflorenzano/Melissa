## Requisitos

## Melissa — Servidor Web

Este repositório contém o servidor web da assistente Melissa e arquivos para execução via Docker/Compose.

**Resumo:** o servidor é uma API em .NET 9 que expõe endpoints REST e um hub SignalR (`/melissa`) para comunicação em tempo real. O projeto integra ferramentas locais (Whisper/Whisper.ggml para transcrição, EdgeTTS para síntese) e opcionalmente um modelo local via Ollama.

**Onde estão os arquivos importantes:**
- **Compose:** `src/Melissa/compose.yaml`
- **Dockerfile:** `src/Melissa/Dockerfile`
- **Projeto do servidor:** `src/Melissa/Melissa.WebServer`

**Portas usadas:** por padrão o servidor escuta em `8080` (exposto pelo `docker-compose`).

**Atenção:** algumas funcionalidades (integração externa e disponibilidade da assistente) dependem de serviços e chaves externas (por exemplo Ollama e AllUNeed API). O comportamento sem essas dependências é tratado com warnings no log.

**Licença / Créditos:** ver arquivo `LICENSE`.

**Conteúdo deste README:** Visão Geral, Requisitos, Instalação Local, Execução com Docker/Compose, Manual do Usuário (uso dos endpoints) e Troubleshooting.

**
**Visão Geral**

O servidor fornece:
- Um hub SignalR em `POST /melissa` (rotas SignalR padrão) para conexões em tempo real.
- Endpoint de áudio para enviar áudio bruto e receber retorno sintetizado: `POST /melissa/AskMelissaAudio`.
- Endpoints utilitários e de gerenciamento de tarefas (GET/POST) listados na seção de Uso.

**
**Requisitos**

- **Sistema:** Linux, macOS ou Windows com suporte a .NET 9 e Docker (se optar por containers).
- **.NET SDK:** versão compatível com `net9.0` (instale o SDK .NET 9).
- **Opcionais:**
	- Ollama (se desejar usar modelos locais via Ollama). Instale por: https://ollama.com/download
	- `docker` e `docker-compose` (ou `docker compose`) se for executar por contêiner.

**Variáveis e arquivos importantes**
- `HolidaysCsvPath`: caminho para `holidays_2025.csv` (padrão em `src/Melissa/Melissa.WebServer/Data/holidays_2025.csv`).
- `AllUNeedApiUrl` e `AllUNeedApiKey`: URL e chave da API externa (opcional). Sem a chave algumas funcionalidades podem não funcionar completamente.
- `OLLAMA_URL`: URL do serviço Ollama quando executado via container Compose.

**
**Instalação Local (desenvolvimento)**

1) Instale o .NET 9 SDK a partir do site oficial: https://dotnet.microsoft.com/

2) Instale Ollama e prepare modelos locais conforme sua necessidade.

3) Restaurar e executar localmente (a partir da raiz do repositório):

```bash
cd src/Melissa
dotnet restore Melissa.WebServer/Melissa.WebServer.csproj
dotnet build Melissa.WebServer/Melissa.WebServer.csproj -c Debug
dotnet run --project Melissa.WebServer/Melissa.WebServer.csproj
```

Observações:
- O servidor carrega um arquivo CSV de feriados (`HolidaysCsvPath`) ao iniciar; se o arquivo não existir, o sistema emite um warning mas continua a execução.
- Para desenvolvimento, ajuste `appsettings.Development.json` ou variáveis de ambiente para alterar configurações (por exemplo `AllUNeedApiKey`).

**
**Execução com Docker / Docker Compose**

O repositório inclui um `Dockerfile` e um `compose.yaml` em `src/Melissa` que definem dois serviços:
- `melissa.webserver`: constrói a imagem do servidor e expõe a porta `8080`.
- `ollama`: puxa a imagem oficial `ollama/ollama:latest` (opcional dependendo do uso de modelos locais).

Exemplos de comandos (a partir da raiz do repo):

```bash
# Subir com build (usa o compose em src/Melissa)
cd src/Melissa
docker compose -f compose.yaml up --build

# Para rodar em background
docker compose -f compose.yaml up -d --build

# Parar e remover containers
docker compose -f compose.yaml down
```

Variáveis de ambiente no `compose.yaml` (padrões):
- `HolidaysCsvPath=/app/Data/holidays_2025.csv`
- `AllUNeedApiUrl=https://alluneed.com.br/api/`
- `AllUNeedApiKey=` (deixe vazio para não usar)
- `OLLAMA_URL=http://ollama:11434`

Se quiser passar uma chave `AllUNeedApiKey`, defina no `compose.yaml` ou via variáveis de ambiente antes de subir os containers.

**
**Manual do Usuário — Endpoints e Exemplos**

Observação: o servidor expõe endpoints HTTP conforme implementado em `Program.cs` e em `AppEndpoints.cs`.

- **Health check**
	- **GET** `http://localhost:8080/health`
	- Retorno: `200 OK` com `OK` quando a assistente está disponível; `500` com mensagem de status quando não.
	- Exemplo:

```bash
curl http://localhost:8080/health
```

- **Hub SignalR**
	- Caminho: `/melissa` (SignalR Hub). Use o cliente SignalR para conectar e trocar mensagens em tempo real.
	- Exemplo com JavaScript (signalr client):

```js
import * as signalR from '@microsoft/signalr';
const connection = new signalR.HubConnectionBuilder()
	.withUrl('http://localhost:8080/melissa')
	.build();
await connection.start();
```

- **Audio — Enviar áudio e receber resposta sintetizada**
	- **POST** `http://localhost:8080/melissa/AskMelissaAudio`
	- Corpo: áudio em PCM raw (16kHz, 16-bit, mono). O endpoint gera internamente um WAV, usa Whisper para transcrever e responde em `audio/mpeg` com síntese TTS.
	- Exemplo (envia arquivo raw PCM):

```bash
curl --data-binary @audio.pcm \
	-H "Content-Type: application/octet-stream" \
	http://localhost:8080/melissa/AskMelissaAudio --output reply.mp3
```

	- Observação: o cliente desktop do projeto usa streaming SignalR para áudio em `MelissaHub` (veja `Melissa.DesktopClient` e `Melissa.DesktopAvaloniaClient`).

- **Ferramentas e Tarefas** (rotas úteis)
	- **GET** `/melissa/GetCurrentTemperatureByLocation?location=<cidade>`
		- Exemplo:

```bash
curl "http://localhost:8080/melissa/GetCurrentTemperatureByLocation?location=Sao%20Paulo"
```

	- **GET** `/melissa/ExportNationalHolidaysToTxt` — exporta feriados nacionais para `.txt` no servidor.

	- **Tarefas (TaskList)**
		- Adicionar nova tarefa:
			- **POST** `/melissa/AddNewTask` — parâmetros: `taskTitle`, `taskDescription` (opcional).
			- Exemplo (form-url-encoded):

```bash
curl -X POST http://localhost:8080/melissa/AddNewTask \
	-d "taskTitle=Comprar%20leite&taskDescription=Supermercado"
```

		- Adicionar item a uma tarefa existente:
			- **POST** `/melissa/AddNewItemTask` — parâmetros: `taskId` (int), `taskDescription`.

```bash
curl -X POST http://localhost:8080/melissa/AddNewItemTask -d "taskId=1&taskDescription=Leite%202L"
```

		- Cancelar item: **POST** `/melissa/CancelTaskItemById` (params: `taskItenId`, `taskId`).
		- Listar tarefas: **GET** `/melissa/GetAllTasks`
		- Listar itens por tarefa: **GET** `/melissa/GetAllItensByTaskId?taskId=1`
		- Completar item: **POST** `/melissa/CompleteItemTask` (param: `taskItenId`)
		- Enviar tarefa por e-mail: **POST** `/melissa/SendTaskByEmail` (params: `email`, `taskId`)
		- Arquivar / desarquivar tarefa: **POST** `/melissa/ArchiveTaskById` / `/melissa/UnarchiveTaskById` (param: `taskId`)

	- **Histórico de conversas por período**
		- **POST** `/melissa/SendEmailConversationHistoryByPeriod` — parâmetros: `email`, `startPeriod`, `endPeriod` (formato ISO ou compatível com bind do .NET).

**
**Configuração e variáveis de ambiente**

- Você pode configurar as opções pelo `appsettings.json` do projeto `Melissa.WebServer` ou por variáveis de ambiente com os mesmos nomes (`HolidaysCsvPath`, `AllUNeedApiUrl`, `AllUNeedApiKey`, `OLLAMA_URL`).
- Quando executado em container, o `compose.yaml` já define valores de exemplo. Ajuste o arquivo `src/Melissa/compose.yaml` conforme necessário.

**
**Troubleshooting (problemas comuns)**

- Erro: faltando `holidays_2025.csv` — verifique `src/Melissa/Melissa.WebServer/Data/holidays_2025.csv` ou ajuste `HolidaysCsvPath`.
- Erro: assistente indisponível — confira logs; se estiver usando Ollama via Compose, verifique se o serviço `ollama` subiu e se `OLLAMA_URL` está correta.
- Erro ao transcrever áudio / falta de modelo Whisper — a aplicação baixa automaticamente o modelo `ggml-medium.bin` se não existir, mas garanta conexão de rede ou providencie o arquivo no diretório de trabalho.
- Problemas com TTS (EdgeTTS): verifique se as dependências e permissões do runtime permitem chamadas ao mecanismo Edge TTS.

**
**Desenvolvimento e testes**

- Para executar testes ou rodar em modo debug, use o Visual Studio / Rider / CLI do .NET apontando para `src/Melissa/Melissa.WebServer/Melissa.WebServer.csproj`.

**
**Ajuda adicional / Contribuição**

- Para dúvidas específicas sobre integração com Ollama, consulte https://ollama.com/docs
- Para reportar bugs ou abrir PRs, use o sistema de issues/pull-requests do repositório no GitHub.
