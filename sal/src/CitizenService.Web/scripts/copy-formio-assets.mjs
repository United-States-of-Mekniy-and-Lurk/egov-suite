import { cp, mkdir } from 'node:fs/promises';

const source = new URL('../node_modules/@formio/js/dist/', import.meta.url);
const destination = new URL('../wwwroot/lib/formio/', import.meta.url);

await mkdir(destination, { recursive: true });
await Promise.all([
  cp(new URL('formio.full.min.js', source), new URL('formio.full.min.js', destination)),
  cp(new URL('formio.full.min.css', source), new URL('formio.full.min.css', destination)),
  cp(new URL('fonts/', source), new URL('fonts/', destination), { recursive: true })
]);