import { OutputBlockData } from "@editorjs/editorjs";
import { createApp, onMounted, ref } from "vue";
import { CustomWindow } from "../types/custom-window";
import edjsParser from "editorjs-html";
import DOMPurify from "dompurify";
import plugins from "../editor-parser/parser";
declare let window: CustomWindow;

const editorParser = edjsParser(plugins);
createApp({
  setup() {
    const data = ref<{
      title: string;
      content: OutputBlockData[];
      contentHtml: string;
      displayDate: string;
    }>({
      title: "",
      content: [],
      contentHtml: "",
      displayDate: "",
    });
    onMounted(() => {
      data.value.title = window.viewData?.post?.title ?? "";
      data.value.content = window.viewData?.post?.content ?? [];

      let htmlBlocks = editorParser.parse({
        blocks: data.value.content,
      });

      let html = htmlBlocks.reduce((a, b) => {
        a += b;
        return a;
      }, "");
      html = DOMPurify.sanitize(html);
      data.value.contentHtml = html;
      const date = new Date(window.viewData?.post?.createdAt);
      data.value.displayDate = date.toLocaleString(undefined, {
        month: "long",
        day: "2-digit",
        year: "numeric",
      });
    });

    return {
      data,
    };
  },
}).mount("#blogPage");
