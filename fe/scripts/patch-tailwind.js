const fs = require("fs");
const path = require("path");

const nodeDist = path.join(__dirname, "../node_modules/@tailwindcss/node/dist");

// Patch index.js
const jsFile = path.join(nodeDist, "index.js");
if (fs.existsSync(jsFile)) {
  let content = fs.readFileSync(jsFile, "utf-8");
  const needle = "function Le(e,r,t){return new Promise((n,i)=>e.resolve({},t,r,{},(l,o)=>{if(l)return i(l);n(o)}))}";
  const replacement = "function Le(e,r,t){return new Promise((n,i)=>e.resolve({},t,r,{},(l,o)=>{if(l)return i(l);n(typeof o===\"string\"?o.replaceAll(\"\\0\",\"\"):o)}))}";
  if (content.includes(needle)) {
    content = content.replace(needle, replacement);
    fs.writeFileSync(jsFile, content, "utf-8");
    console.log("✓ Successfully patched @tailwindcss/node index.js");
  }
}

// Patch index.mjs
const mjsFile = path.join(nodeDist, "index.mjs");
if (fs.existsSync(mjsFile)) {
  let content = fs.readFileSync(mjsFile, "utf-8");
  const needle = "function Oe(e,r,t){return new Promise((n,i)=>e.resolve({},t,r,{},(l,o)=>{if(l)return i(l);n(o)}))}";
  const replacement = "function Oe(e,r,t){return new Promise((n,i)=>e.resolve({},t,r,{},(l,o)=>{if(l)return i(l);n(typeof o===\"string\"?o.replaceAll(\"\\0\",\"\"):o)}))}";
  if (content.includes(needle)) {
    content = content.replace(needle, replacement);
    fs.writeFileSync(mjsFile, content, "utf-8");
    console.log("✓ Successfully patched @tailwindcss/node index.mjs");
  }
}
