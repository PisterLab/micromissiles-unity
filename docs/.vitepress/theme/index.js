import { useData, useRoute } from "vitepress";
import DefaultTheme from "vitepress/theme";
import codeblocksFold from "vitepress-plugin-codeblocks-fold";

import "./custom.css"
import "vitepress-plugin-codeblocks-fold/style/index.css";

export default {
    ...DefaultTheme,
    enhanceApp(ctx) {
        DefaultTheme.enhanceApp(ctx);
    },
    setup() {
        const { frontmatter } = useData();
        const route = useRoute();
        codeblocksFold({ route, frontmatter }, true, 400);
    },
};
