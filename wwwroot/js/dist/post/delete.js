import { _ as __awaiter, a as __generator, S as StatusCodes, t as toastr } from '../toastr-CrEQThWh.js';
import { getCSRF } from '../csrf/get-csrf.js';
import '../_commonjsHelpers-BFTU3MAI.js';

var id = "";
var csrf = getCSRF();
var deleteBtns = document.querySelectorAll(".delete-post-btn");
deleteBtns.forEach(function (btn) {
    btn.addEventListener("click", function (event) {
        var el = event.target;
        id = el.getAttribute("post-id");
    });
});
var deleteConfirmBtn = document.querySelector("#confirmDeleteButton");
deleteConfirmBtn.addEventListener("click", function () { return __awaiter(void 0, void 0, void 0, function () {
    var response, post;
    return __generator(this, function (_a) {
        switch (_a.label) {
            case 0: return [4 /*yield*/, fetch("/App/Post/Delete/".concat(id), {
                    method: "DELETE",
                    headers: new Headers({
                        RequestVerificationToken: csrf,
                    }),
                })];
            case 1:
                response = _a.sent();
                if (response.status === StatusCodes.OK) {
                    toastr.info("Post deleted successfuly");
                    post = document.querySelector("#post-".concat(id));
                    post === null || post === void 0 ? void 0 : post.remove();
                }
                return [2 /*return*/];
        }
    });
}); });
