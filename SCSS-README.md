# SCSS Build Setup for FIPS Frontend

This project uses SCSS for styling with automatic compilation to CSS during the build process.

## Prerequisites

- Node.js (v16 or higher)
- npm

## Setup

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Build CSS:**
   ```bash
   npm run build-css
   ```

3. **Watch for changes (development):**
   ```bash
   npm run watch-css
   ```

## SCSS Structure

```
wwwroot/scss/
├── main.scss              # Main entry point
├── _fips-custom.scss      # Custom GOV.UK overrides
├── _product-tags.scss     # Product state styling
├── _forms.scss           # Form components
└── _tables.scss          # Table styling
```

## Build Process

The SCSS files are automatically compiled during the .NET build process:

- **Development**: `npm run watch-css` runs in the background
- **Production**: `npm run build-css` compiles minified CSS

## Available Scripts

- `npm run build-css` - Compile SCSS to CSS (minified)
- `npm run watch-css` - Watch SCSS files and compile on changes
- `npm run build` - Alias for build-css
- `npm run dev` - Alias for watch-css

## GOV.UK Frontend Integration

The SCSS setup imports the full GOV.UK Frontend library and allows for:

- Custom variable overrides
- Component-specific styling
- Responsive design patterns
- Accessibility-compliant components

## Customization

### Variables
Override GOV.UK variables in `_fips-custom.scss`:
```scss
$govuk-font-family: "GDS Transport", arial, sans-serif;
$fips-primary-color: #1d70b8;
```

### Components
Create new component files in the `scss/` directory and import them in `main.scss`.

### Build Configuration
Modify `package.json` scripts to change compilation options:
- `--style=compressed` for production (minified)
- `--style=expanded` for development (readable)
- `--source-map` for debugging
