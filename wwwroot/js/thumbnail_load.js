(function() {
    const t = ultra.tags;
    let img_url = null;

    ultra.by_id('thumbnail-input').$on_change(function(){
        if (img_url) {
            URL.revokeObjectURL(img_url);
            img_url = null;
        }
        const file = this.files[0];
        if (file) {
            img_url = URL.createObjectURL(file);
        }
        update(file);
    });

    const el = ultra.by_id('thumbnail-container');
    function update(file) {
        if (img_url) {
            el.$replace_all(t.div(
                t.div(t.img(img_url)).$class('preview'),
                t.div(file.name).$class('file-name'),
                t.div(format_file_size(file.size)).$class('file-size'),
                t.button('×')
                    .$class('remove-btn')
                    .$attr('aria-label', 'Удалить файл')
                    .$onclick(() => {
                        remove_thumbnail();
                        update()
                    }),
            ).$class('image-card')).$class_remove('file-loader');
        } else {
            el.$replace_all(t.p('Обложка не прикреплена. Будет использовано первое фото из "Фото товара".')).$class('file-loader');
        }
    }

    function remove_thumbnail() {
        el.value = '';
        if (img_url) {
            URL.revokeObjectURL(img_url);
            img_url = null;
        }
    }

    update();
})();
