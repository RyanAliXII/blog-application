import EditorJS, { OutputBlockData } from "@editorjs/editorjs";
import Header from "@editorjs/header";
import ImageTool from "@editorjs/image";
import CodeTool from "@editorjs/code";
import List from "@editorjs/list";
import Table from "@editorjs/table";
import Editor from "@tinymce/tinymce-vue";
import { StatusCodes } from "http-status-codes";
import toastr from "toastr";
import "toastr/build/toastr.css";
import { createApp, onMounted, ref } from "vue";
import getCSRF from "../../csrf/get-csrf";
import { CustomWindow } from "../../types/custom-window";
declare let window: CustomWindow;

createApp({
  components: {
    editor: Editor,
  },
  setup() {
    const csrf = ref("");
    const errors = ref({});
    const thumbnail = ref<File | null | undefined>(null);
    const form = ref<{
      title: string;
      thumbnail: string;
      content: OutputBlockData[];
    }>({
      title: "",
      thumbnail: "",
      content: [],
    });
    const thumbnailInput = ref<HTMLInputElement | null>(null);
    const uploadImage = async (formData: FormData) => {
      const response = await fetch("/App/Post/UploadFile", {
        method: "POST",
        body: formData,
        headers: new Headers({
          RequestVerificationToken: csrf.value,
        }),
      });
      const data = await response.json();
      return data.location;
    };

    const uploadEditorImage = async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      const location = await uploadImage(formData);
      return `${window.viewData?.s3Url}/${location}`;
    };
    const handleThumbnail = (event: Event) => {
      const input = event.target as HTMLInputElement;
      thumbnail.value = input.files?.[0];
    };

    const editorRef = ref<HTMLDivElement>();
    let editor: InstanceType<typeof EditorJS>;
    const onSubmit = async () => {
      let location = "";
      if (thumbnail.value) {
        const formData = new FormData();
        formData.append("file", thumbnail.value);
        location = await uploadImage(formData);
      }
      const data = await editor.save();
      form.value.content = data.blocks;
      const response = await fetch("/App/Post/Create", {
        method: "POST",
        headers: new Headers({
          "Content-Type": "application/json:",
          RequestVerificationToken: csrf.value,
        }),
        body: JSON.stringify({ ...form.value, thumbnail: location }),
      });
      if (response.status === StatusCodes.OK) {
        toastr.info("Post has been created.");
        form.value.content = [];
        form.value.thumbnail = "";
        form.value.title = "";
        thumbnailInput.value.value = null;
        editor.clear();
      }

      if (response.status === StatusCodes.BAD_REQUEST) {
        const data = await response.json();
        errors.value = data?.errors ?? {};
      }
      if (response.status === StatusCodes.INTERNAL_SERVER_ERROR) {
        toastr.error("Unknown error occured please try again latter");
      }
    };

    onMounted(() => {
      editor = new EditorJS({
        holder: editorRef.value,
        minHeight: 100,
        placeholder: "Write something about your chosen topic",
        tools: {
          header: {
            class: Header,
            inlineToolbar: true,
            config: {
              defaultLevel: 1,
            },
          },
          table: Table,
          image: {
            class: ImageTool,
            config: {
              uploader: {
                uploadByFile(file: File) {
                  return uploadEditorImage(file).then((imageUrl) => ({
                    success: 1,
                    file: {
                      url: imageUrl,
                    },
                  }));
                },
                uploadByUrl(url: string) {
                  return new Promise((resolve) => {
                    resolve({
                      success: 1,
                      file: {
                        url: url,
                      },
                    });
                  });
                },
              },
            },
          },
          code: CodeTool,
          list: {
            class: List,
            inlineToolbar: true,
            config: {
              defaultStyle: "unordered",
            },
          },
        },
      });
      csrf.value = getCSRF();
    });

    return {
      editorRef,
      handleThumbnail,
      onSubmit,
      form,
      errors,
      thumbnailInput,
    };
  },
}).mount("#createPostPage");
