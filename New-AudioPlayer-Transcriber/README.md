# React Audio Player with Transcription
This app is a POC to generate an audio player that will scroll and highlight the words to move with the audio.

## Setup

Here is how the application was setup:
- Vite to create the application.  
- node version 24.11.0
- Make sure you install and compile this before running.
- Data folder has sameple monolog files.  If you want to try other recordings/transcriptions.  Either create a JSON file with the monolog and get the URL link for the audio OR you can create a data call to pull those in.  This was just a test.
- Run "Live Sass Compiler"  To set it up properly, go into VSCode settings and add this for correct structure.
```
 "liveSassCompile.settings.autoprefix": [],
    "liveSassCompile.settings.excludeList": [
        "/**/node_modules/**",
        "/.vscode/**"
    ],
    "liveSassCompile.settings.partialsList": [
        "/**/_*.s[ac]ss"
    ],
    "liveSassCompile.settings.generateMap": false,
    "liveSassCompile.settings.formats": [
        {
            "format": "expanded",
            "extensionName": ".css",
            //"savePath": "/src/assets/css/"
            "savePathReplacementPairs": {"/scss/": "/css/"}
        },
        {
            "format": "compressed",
            "extensionName": ".min.css",
            //"savePath": "/src/assets/css/"
            "savePathReplacementPairs": {"/scss/": "/css/"}
        }
    ],
```



## Future use

Ideally, Echo may use this for review process.  Would like to eventually replace TET/Reporting with a React app.


## React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.
