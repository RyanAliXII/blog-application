import { StatusCodes } from "http-status-codes";
import toastr from "toastr";
import getCSRF from "../../csrf/get-csrf";
import "toastr/build/toastr.css";
let id = "";
const csrf: string = getCSRF();

const deleteBtns: NodeListOf<HTMLButtonElement> =
  document.querySelectorAll(".delete-post-btn");
deleteBtns.forEach((btn) => {
  btn.addEventListener("click", (event) => {
    const el = event.target as HTMLButtonElement;
    id = el.getAttribute("post-id");
  });
});

const deleteConfirmBtn: HTMLButtonElement = document.querySelector(
  "#confirmDeleteButton"
);

deleteConfirmBtn.addEventListener("click", async () => {
  const response = await fetch(`/App/Post/Delete/${id}`, {
    method: "DELETE",
    headers: new Headers({
      RequestVerificationToken: csrf,
    }),
  });

  if (response.status === StatusCodes.OK) {
    toastr.info("Post deleted successfuly");
    const post: HTMLDivElement = document.querySelector(`#post-${id}`);
    post?.remove();
  }
});
