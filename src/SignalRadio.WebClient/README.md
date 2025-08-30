# SignalRadio.WebClient

Minimal Vite + React + TypeScript scaffold for the SignalRadio UI.

Quick start

1. cd src/SignalRadio.WebClient
2. npm install
3. npm run dev

The dev server will run on http://localhost:3000 by default.

Notes
- This is intentionally minimal. The dev server proxies requests from `/api` to http://localhost:5000 by default — start the API on that port or adjust `vite.config.ts`.
- You can use npm or a different package manager (pnpm/yarn) as you prefer.

API configuration
- During development the Vite dev server proxies `/api` to `http://localhost:5000` (see `vite.config.ts`).
- At build/runtime you can set the client base URL using the Vite env var `VITE_API_BASE`.
	- Example: `VITE_API_BASE=https://your-api.example.com`
	- See `.env.example` for a template.
- A small helper is included at `src/api.ts` to centralize fetch calls and error handling.
