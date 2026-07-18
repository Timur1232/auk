'use strict';

(function() {
    const t = ultra.tags;

    let files_state = new ultra.State([], 'files', {
        auto_notify: false,
        update_callback: update,
        after_callback: touch_input_tag,
    });
    let id_counter = 0;

    function revoke_url(obj) {
        if (obj.url) {
            URL.revokeObjectURL(obj.url);
            obj.url = null;
        }
    }

    function update(el, files) {
        el.$clear();

        if (files.length === 0) {
            el.$replace_all(t.span('Файлы не выбраны.').$class('empty-message'));
            return;
        }

        for (const item of files) {
            const card = t.div(
                () => {
                    let child;
                    if (item.file.type.startsWith('image/')) {
                        child = t.img(item.url);
                    } else {
                        child = t.span('🗋').$style('fontSize', '40px');
                    }
                    return t.div(child).$class('preview');
                },
                t.button('×')
                    .$class('remove-btn')
                    .$attr('aria-label', 'Удалить файл')
                    .$onclick(() => {
                        const id = item.id;
                        remove_file_by_id(id);
                    }),
            ).$class('card');

            el.$append(card);
        }
    }

    function add_files(new_files) {
        if (!new_files || new_files.length === 0) return;

        const new_items = [];
        for (const file of new_files) {
            const url = URL.createObjectURL(file);
            new_items.push({
                id: id_counter++,
                file: file,
                url: url,
            });
        }

        files_state.val.splice(0, 0, ...new_items);
        files_state.notify();
    }

    // Update input tag for change event to work when loading same file after removing from list
    // We have files state fully in javascript inside files_state variable, and input tag doesn't know when we change the state
    function touch_input_tag() {
        file_input.value = [];
    }

    function remove_file_by_id(id) {
        const index = files_state.val.findIndex(it => it.id === id);
        if (index === -1) return;
        revoke_url(files_state.val[index]);
        files_state.val.splice(index, 1);
        files_state.notify();
    }

    const file_input = ultra.by_id('files-input');
    ultra.from_dom(file_input.closest('form.form')).$on_submit(() => {
        const dt = new DataTransfer();
        for (const { file } of files_state.val) {
            dt.items.add(file);
        }
        file_input.files = dt.files;
    });

    ultra.by_id('add-files').$on_click(() => {
        file_input.click();
    });

    ultra.by_id('clear-files').$on_click(() => {
        files_state.val.forEach(revoke_url);
        files_state.val = [];
        files_state.notify();
    });

    file_input.$on_change(e => {
        e.preventDefault();
        const selected_files = e.target.files;
        if (selected_files && selected_files.length > 0) {
            add_files(selected_files);
        }
    });

    files_state.notify();
})()
