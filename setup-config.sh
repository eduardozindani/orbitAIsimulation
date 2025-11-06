#!/bin/bash

# Orbit AI Simulation - Quick Setup Script
# This script helps you set up API key configuration files from templates

echo "=========================================="
echo "Orbit AI Simulation - Configuration Setup"
echo "=========================================="
echo ""

# Check if running from correct directory
if [ ! -f "README.md" ] || [ ! -d "Assets" ]; then
    echo "❌ Error: Please run this script from the project root directory"
    exit 1
fi

echo "This script will help you set up API key configuration files."
echo ""

# Function to copy template if target doesn't exist
copy_template() {
    local template=$1
    local target=$2

    if [ -f "$target" ]; then
        echo "⚠️  $target already exists - skipping"
    elif [ ! -f "$template" ]; then
        echo "❌ Template not found: $template"
    else
        cp "$template" "$target"
        echo "✅ Created: $target"
    fi
}

# Copy all template files
echo "Copying template files to create configuration assets..."
echo ""

copy_template "Assets/Resources/OpenAISettings.asset.template" "Assets/Resources/OpenAISettings.asset"
copy_template "Assets/Resources/Agents/ElevenLabsSettings.asset.template" "Assets/Resources/Agents/ElevenLabsSettings.asset"
copy_template "Assets/AnastasiaVoiceSettings.asset.template" "Assets/AnastasiaVoiceSettings.asset"
copy_template "Assets/HubbleVoiceSettings.asset.template" "Assets/HubbleVoiceSettings.asset"
copy_template "Assets/KarlVoiceSettings.asset.template" "Assets/KarlVoiceSettings.asset"

# Create .env if it doesn't exist
if [ -f ".env" ]; then
    echo "⚠️  .env already exists - skipping"
else
    cp ".env.example" ".env"
    echo "✅ Created: .env"
fi

echo ""
echo "=========================================="
echo "✅ Configuration files created!"
echo "=========================================="
echo ""
echo "NEXT STEPS:"
echo ""
echo "1. Add your API keys to the configuration files:"
echo "   Option A (Recommended): Edit .env file"
echo "   Option B: Edit .asset files in Unity Editor"
echo ""
echo "2. Get your API keys:"
echo "   • OpenAI:      https://platform.openai.com/api-keys"
echo "   • ElevenLabs:  https://elevenlabs.io/app/settings/api-keys"
echo ""
echo "3. Open the project in Unity 6000.0.47f1"
echo ""
echo "4. See README.md for detailed instructions"
echo ""
echo "⚠️  IMPORTANT: Never commit files with real API keys!"
echo "    These files are gitignored by default."
echo ""
