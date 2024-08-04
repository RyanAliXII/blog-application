import { nodeResolve } from "@rollup/plugin-node-resolve";
import replace from "@rollup/plugin-replace";
import alias from "@rollup/plugin-alias";
import typescript from "@rollup/plugin-typescript";
import commonjs from "@rollup/plugin-commonjs";
import css from "rollup-plugin-import-css";
export default {
  input: {
    "post/create": "wwwroot/js/app/post/create.ts",
    "post/edit": "wwwroot/js/app/post/edit.ts",
    "post/delete": "wwwroot/js/app/post/delete.ts",
    "csrf/get-csrf": "wwwroot/js/csrf/get-csrf.ts",
    "blog/blog-post": "wwwroot/js/blog/blog-post.ts",
    "flowbite/flowbite": "wwwroot/js/flowbite/flowbite.js",
  },
  output: {
    dir: "wwwroot/js/dist",
    manualChunks: {
      "vendor/vue": ["vue"],
    },
    format: "es",
  },

  plugins: [
    typescript(),
    nodeResolve({ preferBuiltins: false }),
    commonjs(),
    css({ inject: true }),
    replace({
      preventAssignment: true,
      "process.env.NODE_ENV": JSON.stringify("development"),
      __VUE_OPTIONS_API__: "true",
      __VUE_PROD_DEVTOOLS__: "false",
      __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: "false",
    }),
    alias({
      entries: [
        {
          find: "vue",
          replacement: "vue/dist/vue.esm-bundler.js",
        },
      ],
    }),
  ],
};
