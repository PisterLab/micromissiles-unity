import { defineConfig } from "vitepress";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { readFileSync } from "node:fs";

const __dirname = dirname(fileURLToPath(import.meta.url));

// https://vitepress.dev/reference/site-config
export default defineConfig({
  markdown: {
    math: true,
    async shikiSetup(shiki) {
      const loaded = shiki.getLoadedLanguages();
      if (loaded.includes("textproto")) {
        return;
      }
      const grammarPath = join(__dirname, "syntaxes/textproto.tmLanguage.json");
      const grammar = JSON.parse(readFileSync(grammarPath, "utf-8"));
      grammar.name = grammar.name || "textproto";
      grammar.scopeName = grammar.scopeName || "source.textproto";
      const aliases = new Set(["pbtxt", ...(grammar.aliases || [])]);
      aliases.delete("textproto");
      grammar.aliases = Array.from(aliases);
      await shiki.loadLanguage(grammar);
    },
    codeLangAliases: {
      pbtxt: "textproto"
    }
  },

  title: "micromissiles-unity",
  description: "Swarm-on-swarm simulator using micromissiles for point defense",
  base: "/micromissiles-unity/",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: "Home", link: "/" },
      { text: "Simulator Overview", link: "/Simulator_Overview" },
      { text: "Development Guide", link: "/Development_Guide" }
    ],

    sidebar: [ 
      {
        text: "Documentation",
        items: [
          { text: "Simulator Overview", link: "/Simulator_Overview" },
          { text: "Keybinds and Controls", link: "/Keybinds_and_Controls" },
          { text: "Simulation Configuration Guide", link: "/Simulation_Configuration_Guide" },
          { text: "Simulation Logging", link: "/Simulation_Logging" },
          { text: "Coverage Reports", 
            items: [
              { text: "EditMode Tests", link: "https://pisterlab.github.io/micromissiles-unity/coverage/editmode/Report/index.html" },
              { text: "PlayMode Tests", link: "https://pisterlab.github.io/micromissiles-unity/coverage/playmode/Report/index.html" }
            ]
          },
          { text: "Development Guide", link: "/Development_Guide" },
          { text: "Plugins Guide", link: "/Plugins_Guide" }
        ]
      }
    ],

    socialLinks: [
      { icon: "github", link: "https://github.com/PisterLab/micromissiles-unity" }
    ],
    search: {
      provider: "local"
    },
    footer: {
      message: "Released under the <a href='https://github.com/PisterLab/micromissiles-unity/blob/main/LICENSE'>BSD-3-Clause License</a>.",
      copyright: "Copyright © 2024-present The Regents of the University of California. All Rights Reserved."
    }
  }
});
