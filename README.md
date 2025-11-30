# Sistema de Cronometragem para Corridas de Moto

Sistema completo para gerenciamento e cronometragem de corridas de moto, desenvolvido em C# com .NET 8.

## Modalidades Suportadas

- **Enduro** - Com especiais cronometradas (entrada/saída)
- **Motocross** - Circuito fechado
- **Cross Country** - Longa distância
- **SuperMoto** - Misto asfalto/terra
- **Motovelocidade** - Alta velocidade

## Tecnologias

- .NET 8
- ASP.NET Core (API REST)
- .NET MAUI (Mobile, Desktop e Coletor Android)
- MySQL 8.0
- Entity Framework Core
- JWT Authentication
- SignalR (Tempo Real)
- SQLite (armazenamento offline nos coletores)

## Funcionalidades

### Sistema 1: API REST
- Autenticação JWT com roles
- Gerenciamento de eventos e categorias
- Processamento de pagamentos PIX
- Cálculo automático de resultados (Enduro e Circuito)
- SignalR para tempo real
- Sincronização de coletores offline

### Sistema 2: App Coletor Android
- Leitura de porta serial (USB-to-Serial)
- Captura de passagens na pista
- Modo offline com fila de sincronização
- SQLite local para armazenamento
- Identificação de dispositivo e ponto de leitura
- Interface simples para operadores

### Sistema 3: App Mobile (Pilotos/Público)
- Inscrição online em eventos
- Pagamento via PIX com QR Code
- Visualização de resultados em tempo real
- Acompanhamento por categoria
- Histórico de corridas
- Notificações push

### Sistema 4: Desktop (Organizadores)
- CRUD completo (pilotos, eventos, categorias)
- Monitoramento de todos os coletores
- Painel de cronometragem ao vivo
- Correção de leituras
- Geração de relatórios (PDF/Excel)
- Dashboard com estatísticas

## Como Executar

### Pré-requisitos
- Visual Studio 2022 ou superior
- .NET 8 SDK
- MySQL 8.0
- Git
- Android SDK (para App Coletor)
- Dispositivo Android ou emulador

### Configuração
(Instruções detalhadas em breve)

## Requisitos para App Coletor Android

- Android 7.0 ou superior
- Cabo USB OTG
- Permissões: USB Host, Network, Storage
- Coletor físico com saída USB/Serial

## Documentação

A documentação completa está na pasta `docs/`

## Autor

Desenvolvido por Gleidson Guilherme de Souza

