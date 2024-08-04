export const getCSRF = () => {
  const input = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  ) as HTMLInputElement | null;

  return input?.value ?? "";
};
export default getCSRF;
