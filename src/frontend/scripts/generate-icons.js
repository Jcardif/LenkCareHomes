#!/usr/bin/env node
/* eslint-disable @typescript-eslint/no-require-imports */
/**
 * LenkCare Homes - Favicon & Icon Generation Script
 * 
 * This script generates PNG files and favicon.ico from SVG sources.
 * 
 * Prerequisites:
 *   npm install sharp to-ico
 * 
 * Usage:
 *   node scripts/generate-icons.js
 */

const sharp = require('sharp');
const fs = require('fs');
const path = require('path');

const PUBLIC_DIR = path.join(__dirname, '../public');

const icons = [
  // Favicons
  { input: 'favicon-16x16.svg', output: 'favicon-16x16.png', size: 16 },
  { input: 'favicon-32x32.svg', output: 'favicon-32x32.png', size: 32 },
  { input: 'favicon.svg', output: 'favicon-48x48.png', size: 48 },
  
  // Apple Touch Icon
  { input: 'apple-touch-icon.svg', output: 'apple-touch-icon.png', size: 180 },
  
  // Android Chrome Icons
  { input: 'android-chrome-192x192.svg', output: 'android-chrome-192x192.png', size: 192 },
  { input: 'android-chrome-512x512.svg', output: 'android-chrome-512x512.png', size: 512 },
];

async function generatePNGs() {
  console.log('🎨 Generating PNG icons from SVG sources...\n');

  for (const icon of icons) {
    const inputPath = path.join(PUBLIC_DIR, icon.input);
    const outputPath = path.join(PUBLIC_DIR, icon.output);

    try {
      await sharp(inputPath)
        .resize(icon.size, icon.size)
        .png()
        .toFile(outputPath);
      
      console.log(`  ✅ ${icon.output} (${icon.size}x${icon.size})`);
    } catch (error) {
      console.error(`  ❌ Failed to generate ${icon.output}:`, error.message);
    }
  }
}

async function generateFaviconICO() {
  console.log('\n🔷 Generating favicon.ico (multi-size)...\n');

  try {
    const pngToIco = require('png-to-ico');
    
    // Generate PNG buffers at different sizes
    const sizes = [16, 32, 48];
    const pngBuffers = await Promise.all(
      sizes.map(async (size) => {
        const svgPath = size === 16 
          ? path.join(PUBLIC_DIR, 'favicon-16x16.svg')
          : path.join(PUBLIC_DIR, 'favicon.svg');
        
        return sharp(svgPath)
          .resize(size, size)
          .png()
          .toBuffer();
      })
    );

    // Convert to ICO
    const icoBuffer = await pngToIco(pngBuffers);
    fs.writeFileSync(path.join(PUBLIC_DIR, 'favicon.ico'), icoBuffer);
    
    console.log('  ✅ favicon.ico (16x16, 32x32, 48x48)');
  } catch (error) {
    console.error('  ❌ Failed to generate favicon.ico:', error.message);
    console.log('     Install dependencies: npm install png-to-ico');
  }
}

async function main() {
  console.log('╔══════════════════════════════════════════════════╗');
  console.log('║       LenkCare Homes - Icon Generator            ║');
  console.log('╚══════════════════════════════════════════════════╝\n');

  await generatePNGs();
  await generateFaviconICO();

  console.log('\n✨ Icon generation complete!\n');
  console.log('Generated files:');
  console.log('  • favicon.ico (multi-size: 16/32/48)');
  console.log('  • favicon-16x16.png');
  console.log('  • favicon-32x32.png');
  console.log('  • apple-touch-icon.png (180x180)');
  console.log('  • android-chrome-192x192.png');
  console.log('  • android-chrome-512x512.png');
}

main().catch(console.error);
