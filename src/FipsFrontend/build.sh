#!/bin/bash

# FIPS Frontend Build Script
echo "Building FIPS Frontend..."

# Install npm dependencies if node_modules doesn't exist
if [ ! -d "node_modules" ]; then
    echo "Installing npm dependencies..."
    npm install
fi

# Build SCSS to CSS
echo "Compiling SCSS to CSS..."
npm run build-css

echo "Build complete!"
