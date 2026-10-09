import fs from "node:fs";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const sharp = require(
  "/Users/dimagordeev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp",
);

if (process.argv.length !== 4) {
  throw new Error("usage: svg_to_png.mjs INPUT.svg OUTPUT.png");
}

const input = fs.readFileSync(process.argv[2]);
await sharp(input, { density: 220 }).png().toFile(process.argv[3]);
