@echo off
echo ========================================
echo    Telemetry Analyzer Pro - Build
echo ========================================

echo.
echo Limpando build anterior...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "bin\Debug" rmdir /s /q "bin\Debug"

echo.
echo Restaurando pacotes NuGet...
dotnet restore

echo.
echo Compilando aplicacao...
dotnet build --configuration Release --no-restore

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERRO: Falha na compilacao!
    pause
    exit /b 1
)

echo.
echo Publicando aplicacao...
dotnet publish --configuration Release --output "publish\TelemetryAnalyzer" --self-contained true --runtime win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=false

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERRO: Falha na publicacao!
    pause
    exit /b 1
)

echo.
echo Copiando arquivos de configuracao...
xcopy "NLog.config" "publish\TelemetryAnalyzer\" /Y
if exist "Assets" xcopy "Assets\*" "publish\TelemetryAnalyzer\Assets\" /E /I /Y

echo.
echo ========================================
echo   BUILD CONCLUIDO COM SUCESSO!
echo ========================================
echo.
echo Executavel criado em: publish\TelemetryAnalyzer\
echo.
pause

// Install.bat - Script de instalação
@echo off
echo ========================================
echo Telemetry Analyzer Pro - Instalacao
echo ========================================

echo.
echo Verificando pre-requisitos...

:: Verificar .NET 8 Runtime
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: .NET 8 Runtime nao encontrado!
    echo.
    echo Por favor, instale o .NET 8 Runtime de:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo .NET Runtime encontrado.

:: Criar diretorios
echo.
echo Criando diretorios...
if not exist "Data" mkdir "Data"
if not exist "Logs" mkdir "Logs"
if not exist "Exports" mkdir "Exports"

:: Verificar Visual C++ Redistributable
echo.
echo Verificando Visual C++ Redistributable...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo AVISO: Visual C++ Redistributable pode estar em falta.
    echo Se o programa nao funcionar, instale de:
    echo https://aka.ms/vs/17/release/vc_redist.x64.exe
    echo.
)

echo.
echo ========================================
echo    INSTALACAO CONCLUIDA!
echo ========================================
echo.
echo Para usar o programa:
echo 1. Execute TelemetryAnalyzer.exe
echo 2. Inicie ACC, Le Mans Ultimate ou iRacing
echo 3. Clique em 'Connect' no programa
echo.
echo Arquivos salvos em: Data\
echo Logs salvos em: Logs\
echo.
pause

// README.md
# Telemetry Analyzer Pro

Análise profissional de telemetria para simuladores de corrida.

## Funcionalidades

### 🏁 Telemetria em Tempo Real
- Conexão automática com ACC, Le Mans Ultimate e iRacing
- Visualização em tempo real dos dados do carro
- Mapa da pista com posição atual
- Gráficos de velocidade, RPM, pedais e marchas

### 📊 Análise de Voltas
- Análise detalhada de cada volta
- Detecção automática de erros (frenagem tardia, traçado ruim)
- Métricas de performance completas
- Análise de setores e curvas

### ⚖️ Comparação de Voltas
- Compare suas voltas com outros pilotos
- Visualização de diferenças no mapa da pista
- Análise de onde ganhar/perder tempo
- Sugestões de melhoria automáticas

### 📁 Importação de Dados
- Suporte a arquivos CSV, LDX e JSON
- Importação de múltiplos arquivos
- Validação automática de dados

## Instalação

### Pré-requisitos
- Windows 10/11 (64-bit)
- .NET 8 Runtime
- Visual C++ Redistributable 2022

### Passos
1. Baixe e extraia o arquivo ZIP
2. Execute `Install.bat` como administrador
3. Execute `TelemetryAnalyzer.exe`

## Como Usar

### Primeira Execução
1. Inicie seu simulador favorito (ACC, LMU ou iRacing)
2. Abra o Telemetry Analyzer Pro
3. Selecione o simulador ou deixe em "Auto-Detect"
4. Clique em "Connect"

### Análise de Telemetria
1. Vá para a aba "Lap Analysis"
2. Selecione uma sessão na lista
3. Escolha uma volta para analisar
4. Clique em "Analyze Selected"

### Comparação de Voltas
1. Vá para a aba "Compare Laps"
2. Selecione duas voltas diferentes
3. Clique em "Compare"
4. Analise as diferenças no mapa e gráficos

## Simuladores Suportados

- ✅ Assetto Corsa Competizione
- ✅ Le Mans Ultimate  
- ✅ iRacing
- 🔄 rFactor 2 (em desenvolvimento)

## Suporte

Em caso de problemas:
1. Verifique os logs em `Logs/`
2. Certifique-se que o simulador está rodando
3. Execute como administrador se necessário

## Versão
1.0.0 - Primeira versão estável