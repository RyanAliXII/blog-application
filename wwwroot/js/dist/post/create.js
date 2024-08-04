import { _ as __awaiter, a as __generator, S as StatusCodes, t as toastr, b as __assign } from '../toastr-CrEQThWh.js';
import { E as Editor, T as Ts, c, I, P, d, a as d$1 } from '../Editor-5fdTFU1X.js';
import { c as createApp, r as ref, o as onMounted } from '../vendor/vue-DtCupTCT.js';
import { getCSRF } from '../csrf/get-csrf.js';
import '../_commonjsHelpers-BFTU3MAI.js';

createApp({
    components: {
        editor: Editor,
    },
    setup: function () {
        var _this = this;
        var csrf = ref("");
        var errors = ref({});
        var thumbnail = ref(null);
        var form = ref({
            title: "",
            thumbnail: "",
            content: [],
        });
        var thumbnailInput = ref(null);
        var uploadImage = function (formData) { return __awaiter(_this, void 0, void 0, function () {
            var response, data;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, fetch("/App/Post/UploadFile", {
                            method: "POST",
                            body: formData,
                            headers: new Headers({
                                RequestVerificationToken: csrf.value,
                            }),
                        })];
                    case 1:
                        response = _a.sent();
                        return [4 /*yield*/, response.json()];
                    case 2:
                        data = _a.sent();
                        return [2 /*return*/, data.location];
                }
            });
        }); };
        var uploadEditorImage = function (file) { return __awaiter(_this, void 0, void 0, function () {
            var formData, location;
            var _a;
            return __generator(this, function (_b) {
                switch (_b.label) {
                    case 0:
                        formData = new FormData();
                        formData.append("file", file);
                        return [4 /*yield*/, uploadImage(formData)];
                    case 1:
                        location = _b.sent();
                        return [2 /*return*/, "".concat((_a = window.viewData) === null || _a === void 0 ? void 0 : _a.s3Url, "/").concat(location)];
                }
            });
        }); };
        var handleThumbnail = function (event) {
            var _a;
            var input = event.target;
            thumbnail.value = (_a = input.files) === null || _a === void 0 ? void 0 : _a[0];
        };
        var editorRef = ref();
        var editor;
        var onSubmit = function () { return __awaiter(_this, void 0, void 0, function () {
            var location, formData, data, response, data_1;
            var _a;
            return __generator(this, function (_b) {
                switch (_b.label) {
                    case 0:
                        location = "";
                        if (!thumbnail.value) return [3 /*break*/, 2];
                        formData = new FormData();
                        formData.append("file", thumbnail.value);
                        return [4 /*yield*/, uploadImage(formData)];
                    case 1:
                        location = _b.sent();
                        _b.label = 2;
                    case 2: return [4 /*yield*/, editor.save()];
                    case 3:
                        data = _b.sent();
                        form.value.content = data.blocks;
                        return [4 /*yield*/, fetch("/App/Post/Create", {
                                method: "POST",
                                headers: new Headers({
                                    "Content-Type": "application/json:",
                                    RequestVerificationToken: csrf.value,
                                }),
                                body: JSON.stringify(__assign(__assign({}, form.value), { thumbnail: location })),
                            })];
                    case 4:
                        response = _b.sent();
                        if (response.status === StatusCodes.OK) {
                            toastr.info("Post has been created.");
                            form.value.content = [];
                            form.value.thumbnail = "";
                            form.value.title = "";
                            thumbnailInput.value.value = null;
                            editor.clear();
                        }
                        if (!(response.status === StatusCodes.BAD_REQUEST)) return [3 /*break*/, 6];
                        return [4 /*yield*/, response.json()];
                    case 5:
                        data_1 = _b.sent();
                        errors.value = (_a = data_1 === null || data_1 === void 0 ? void 0 : data_1.errors) !== null && _a !== void 0 ? _a : {};
                        _b.label = 6;
                    case 6:
                        if (response.status === StatusCodes.INTERNAL_SERVER_ERROR) {
                            toastr.error("Unknown error occured please try again latter");
                        }
                        return [2 /*return*/];
                }
            });
        }); };
        onMounted(function () {
            editor = new Ts({
                holder: editorRef.value,
                minHeight: 100,
                placeholder: "Write something about your chosen topic",
                tools: {
                    header: {
                        class: c,
                        inlineToolbar: true,
                        config: {
                            defaultLevel: 1,
                        },
                    },
                    table: I,
                    image: {
                        class: P,
                        config: {
                            uploader: {
                                uploadByFile: function (file) {
                                    return uploadEditorImage(file).then(function (imageUrl) { return ({
                                        success: 1,
                                        file: {
                                            url: imageUrl,
                                        },
                                    }); });
                                },
                                uploadByUrl: function (url) {
                                    return new Promise(function (resolve) {
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
                    code: d,
                    list: {
                        class: d$1,
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
            editorRef: editorRef,
            handleThumbnail: handleThumbnail,
            onSubmit: onSubmit,
            form: form,
            errors: errors,
            thumbnailInput: thumbnailInput,
        };
    },
}).mount("#createPostPage");
