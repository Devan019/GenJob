#!/bin/bash

# Setup script for ATS environment variables
echo "Setting up ATS environment variables..."

# Create .env file if it doesn't exist
if [ ! -f .env ]; then
    echo "Creating .env file from template..."
    cp environment.template .env
    echo "✅ .env file created from template"
else
    echo "⚠️  .env file already exists"
fi

echo ""
echo "📝 Please edit the .env file and add your API keys:"
echo "   - SHARPAPI_KEY=your_actual_sharpapi_key"
echo "   - MAGICALAPI_KEY=your_actual_magicalapi_key"
echo ""
echo "🔧 You can also set these as system environment variables:"
echo "   export SHARPAPI_KEY=your_actual_sharpapi_key"
echo "   export MAGICALAPI_KEY=your_actual_magicalapi_key"
echo ""
echo "🚀 After setting up your API keys, restart the application"
