This ClientApp will be created with Vite (React + TypeScript) and TailwindCSS.

Run these commands in PowerShell from the solution root (D:\URL-Shortener\MarketConnect):

1) Create project with Vite (React + TS)
> pnpm create vite@latest ClientApp -- --template react-ts

(If you don't have pnpm, use: npm create vite@latest ClientApp -- --template react-ts)

2) Enter folder and install deps
> cd ClientApp
> pnpm install

3) Install TailwindCSS (and peer deps) and initialize
> pnpm add -D tailwindcss postcss autoprefixer
> pnpm exec tailwindcss init -p

4) Install Axios and react-router-dom
> pnpm add axios react-router-dom

5) Tailwind config: update content to include src
(see ClientApp/tailwind.config.cjs in this repo)

6) Add Tailwind imports to main CSS
(see ClientApp/src/index.css)

7) Start dev server
> pnpm dev

Files generated below provide minimal router + Axios setup.
