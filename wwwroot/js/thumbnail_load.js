(function() {
    const t = ultra.tags;
    let img_url = null;

    ultra.by_id('thumbnail-input').$on_change(function(){
        if (img_url) {
            URL.revokeObjectURL(img_url);
            img_url = null;
        }
        img_url = URL.createObjectURL(this.files[0]);
        render();
    });

    function render() {
        const el = ultra.by_id('thumbnail-container');
        if (img_url) {
            el.$replace_all(t.img(img_url).$attr('style', 'height:300px'));
        } else {
            el.$replace_all(t.p('Обложка не прикреплена.'));
        }
    }
})();
