/**
 * Generate PNG favicon and icon assets from SVG sources
 * Run with: node scripts/generate-icons.mjs
 */

import sharp from 'sharp';
import { readFileSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const publicDir = join(__dirname, '..', 'public');

// Read the SVG files
const faviconSvg = readFileSync(join(publicDir, 'favicon.svg'));
const appleTouchIconSvg = readFileSync(join(publicDir, 'apple-touch-icon.svg'));
const androidChrome192Svg = readFileSync(join(publicDir, 'android-chrome-192x192.svg'));
const androidChrome512Svg = readFileSync(join(publicDir, 'android-chrome-512x512.svg'));

async function generateIcons() {
  console.log('Generating favicon-16x16.png...');
  await sharp(faviconSvg)
    .resize(16, 16)
    .png()
    .toFile(join(publicDir, 'favicon-16x16.png'));

  console.log('Generating favicon-32x32.png...');
  await sharp(faviconSvg)
    .resize(32, 32)
    .png()
    .toFile(join(publicDir, 'favicon-32x32.png'));

  console.log('Generating apple-touch-icon.png (180x180)...');
  await sharp(appleTouchIconSvg)
    .resize(180, 180)
    .png()
    .toFile(join(publicDir, 'apple-touch-icon.png'));

  console.log('Generating android-chrome-192x192.png...');
  await sharp(androidChrome192Svg)
    .resize(192, 192)
    .png()
    .toFile(join(publicDir, 'android-chrome-192x192.png'));

  console.log('Generating android-chrome-512x512.png...');
  await sharp(androidChrome512Svg)
    .resize(512, 512)
    .png()
    .toFile(join(publicDir, 'android-chrome-512x512.png'));

  // Generate favicon.ico with multiple sizes
  console.log('Generating favicon.ico (multi-size)...');
  
  // ICO file header structure - generate multiple sizes for future multi-size ICO support
  // Currently just generates the 32x32 version as most browsers accept PNG
  // TODO: Use a proper ICO library (like 'to-ico') for multi-size support

  // Create ICO file (simplified - just use the 32x32 for now as a PNG)
  // For a proper multi-size ICO, we'd need a specialized library
  // Using the 32x32 version as favicon.ico (most browsers accept PNG)
  await sharp(faviconSvg)
    .resize(32, 32)
    .png()
    .toFile(join(publicDir, 'favicon.ico'));

  console.log('✅ All icons generated successfully!');
}

generateIcons().catch(console.error);
