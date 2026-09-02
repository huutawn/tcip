import fs from "node:fs";
import fsp from "node:fs/promises";

// Fix for environments where directory path contains '#' (e.g. C#)
// and Turbopack/PostCSS inserts a null-byte '\0#' into file paths
function cleanPath(p) {
  if (typeof p === "string" && p.includes("\0")) {
    return p.replaceAll("\0", "");
  }
  return p;
}

const origReadFile = fsp.readFile;
fsp.readFile = function (p, ...args) {
  return origReadFile.call(this, cleanPath(p), ...args);
};

const origStat = fsp.stat;
fsp.stat = function (p, ...args) {
  return origStat.call(this, cleanPath(p), ...args);
};

const origAccess = fsp.access;
fsp.access = function (p, ...args) {
  return origAccess.call(this, cleanPath(p), ...args);
};

const origReadFileSync = fs.readFileSync;
fs.readFileSync = function (p, ...args) {
  return origReadFileSync.call(this, cleanPath(p), ...args);
};

const origStatSync = fs.statSync;
fs.statSync = function (p, ...args) {
  return origStatSync.call(this, cleanPath(p), ...args);
};

const origExistsSync = fs.existsSync;
fs.existsSync = function (p) {
  return origExistsSync.call(this, cleanPath(p));
};

const config = {
  plugins: {
    "@tailwindcss/postcss": {},
  },
};

export default config;
