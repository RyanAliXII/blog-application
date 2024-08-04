const plugins = {
  image: (block: any) => {
    if (block.data.stretched) {
      return `<img src="${block.data.file.url}" class="w-full h-auto" alt="${block.data.caption}">`;
    } else {
      return `<img src="${block.data.file.url}" alt="${block.data.caption}" >`;
    }
  },
  table: (block: any) => {
    let markup = `<table class="w-full text-sm text-left rtl:text-right text-gray-500 dark:text-gray-400">`;
    let data = block.data;
    let startIdx = 0;
    if (data.withHeadings) {
      let thead = `<thead class="text-xs  text-gray-700  bg-gray-50 dark:bg-gray-700 dark:text-gray-400">
          <tr>
      `;
      const firstRow = data.content?.[0] ?? [];
      firstRow.forEach((cell) => {
        thead += `<th scope="col" class="px-6 py-3">
                    ${cell}
                  </th>`;
      });
      startIdx = 1;
      thead += `</tr></thead>`;
      markup += thead;
    }

    let tbody = "<tbody>";
    for (let i = startIdx; i < data.content.length; i++) {
      const row = data.content[i];

      tbody += `<tr class="bg-white border-b text-center dark:bg-gray-800 dark:border-gray-700">`;
      row.forEach((cell) => {
        tbody += `<td class="px-6 py-4">
                    ${cell}
                   </td>`;
      });
      tbody += `</tr>`;
    }
    tbody += `</tbody>`;
    markup += tbody;
    markup += `</table>`;
    return markup;
  },
};
export default plugins;
