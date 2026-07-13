#!/usr/bin/env node
import { accessSync, constants, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const manifestPath = join(repoRoot, 'scripts', 'protected-paths.json');
const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));

const errors = [];

for (const relativePath of manifest.paths) {
  const absolutePath = join(repoRoot, relativePath);
  try {
    accessSync(absolutePath, constants.F_OK);
  } catch {
    errors.push(`Missing protected path: ${relativePath}`);
  }
}

for (const relativePath of manifest.requiredFiles) {
  const absolutePath = join(repoRoot, relativePath);
  try {
    accessSync(absolutePath, constants.R_OK);
  } catch {
    errors.push(`Missing protected file: ${relativePath}`);
  }
}

if (errors.length > 0) {
  console.error('Protected path check failed:\n');
  for (const error of errors) {
    console.error(`  - ${error}`);
  }
  console.error('\nThe research/ folder is permanent project data. Restore it from git history if needed.');
  process.exit(1);
}

console.log(`Protected path check passed (${manifest.requiredFiles.length} files, ${manifest.paths.length} paths).`);
