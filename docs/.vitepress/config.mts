import { defineConfig } from "vitepress";

// https://vitepress.dev/reference/site-config
export default defineConfig({
  markdown: {
    math: true
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
          { text: "Simulation Config Guide", link: "/Simulation_Config_Guide" },
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
      copyright: "Copyright © 2024-present by The Regents of the University of California. All Rights Reserved."
    }
  }
});
