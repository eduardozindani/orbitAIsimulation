@echo off
REM Orbit AI Simulation - Quick Setup Script (Windows)
REM This script helps you set up API key configuration files from templates

echo ==========================================
echo Orbit AI Simulation - Configuration Setup
echo ==========================================
echo.

REM Check if running from correct directory
if not exist "README.md" (
    echo Error: Please run this script from the project root directory
    exit /b 1
)

echo This script will help you set up API key configuration files.
echo.

echo Copying template files to create configuration assets...
echo.

REM Copy template files if they don't exist
if exist "Assets\Resources\OpenAISettings.asset" (
    echo Warning: OpenAISettings.asset already exists - skipping
) else (
    copy "Assets\Resources\OpenAISettings.asset.template" "Assets\Resources\OpenAISettings.asset" >nul
    echo Created: Assets\Resources\OpenAISettings.asset
)

if exist "Assets\Resources\Agents\ElevenLabsSettings.asset" (
    echo Warning: ElevenLabsSettings.asset already exists - skipping
) else (
    copy "Assets\Resources\Agents\ElevenLabsSettings.asset.template" "Assets\Resources\Agents\ElevenLabsSettings.asset" >nul
    echo Created: Assets\Resources\Agents\ElevenLabsSettings.asset
)

if exist "Assets\AnastasiaVoiceSettings.asset" (
    echo Warning: AnastasiaVoiceSettings.asset already exists - skipping
) else (
    copy "Assets\AnastasiaVoiceSettings.asset.template" "Assets\AnastasiaVoiceSettings.asset" >nul
    echo Created: Assets\AnastasiaVoiceSettings.asset
)

if exist "Assets\HubbleVoiceSettings.asset" (
    echo Warning: HubbleVoiceSettings.asset already exists - skipping
) else (
    copy "Assets\HubbleVoiceSettings.asset.template" "Assets\HubbleVoiceSettings.asset" >nul
    echo Created: Assets\HubbleVoiceSettings.asset
)

if exist "Assets\KarlVoiceSettings.asset" (
    echo Warning: KarlVoiceSettings.asset already exists - skipping
) else (
    copy "Assets\KarlVoiceSettings.asset.template" "Assets\KarlVoiceSettings.asset" >nul
    echo Created: Assets\KarlVoiceSettings.asset
)

REM Create .env if it doesn't exist
if exist ".env" (
    echo Warning: .env already exists - skipping
) else (
    copy ".env.example" ".env" >nul
    echo Created: .env
)

echo.
echo ==========================================
echo Configuration files created!
echo ==========================================
echo.
echo NEXT STEPS:
echo.
echo 1. Add your API keys to the configuration files:
echo    Option A (Recommended): Edit .env file
echo    Option B: Edit .asset files in Unity Editor
echo.
echo 2. Get your API keys:
echo    - OpenAI:      https://platform.openai.com/api-keys
echo    - ElevenLabs:  https://elevenlabs.io/app/settings/api-keys
echo.
echo 3. Open the project in Unity 6000.0.47f1
echo.
echo 4. See README.md for detailed instructions
echo.
echo WARNING: Never commit files with real API keys!
echo          These files are gitignored by default.
echo.
pause
