const ultra = (function() {
    'use strict';
    const ultra = {
        tag: null,
        tags: {},
        from_dom: null,
        by_id: null,
        by_selector: null,
        by_selector_all: null,
        State: null,
        get_dom_element: null,
    };

    ultra.tag = tag;
    ultra.from_dom = from_dom;
    ultra.dump_tags_to_window = dump_tags_to_window;
    ultra.by_id = by_id;
    ultra.by_selector = by_selector;
    ultra.by_selector_all = by_selector_all;
    ultra.get_dom_element = get_dom_element;

    function tag(name, ...children) {
        const element = from_dom(document.createElement(name));
        element.$append(...children);
        return element;
    }

    const tag_handler = {
        set(target, prop, value) {
            target[prop] = value;
            return true;
        },

        get(target, prop, receiver) {
            if (!prop.startsWith('$')) {
                let field = target[prop];
                if (typeof(field) === 'function') {
                    return (...args) => field.call(target, ...args);
                }
                return field;
            }

            // Native Node object of the DOM
            if (prop === '$node') {
                return target;
            }

            if (prop === '$attr') {
                return (name, value) => {
                    if (value === undefined) value = true; // Assuming boolean attribute
                    target.setAttribute(name, value)
                    return receiver;
                };
            }

            const event_prefix = '$on';
            if (prop.startsWith(event_prefix)) {
                let event_name = prop.slice(event_prefix.length);
                if (event_name[0] === '_') event_name = event_name.slice(1);
                return (callback, ...args) => {
                    target.addEventListener(event_name, callback, ...args);
                    return receiver;
                };
            }

            const class_prefix = '$class';
            if (prop.startsWith(class_prefix)) {
                const command = prop.slice(class_prefix.length+1);
                if (command === '' || command === 'add') {
                    return (...classes) => {
                        target.classList.add(...classes);
                        return receiver;
                    };
                } else if (command === 'remove') {
                    return (...classes) => {
                        target.classList.remove(...classes);
                        return receiver;
                    };
                } else if (command === 'toggle') {
                    return (class_name, cond) => {
                        target.classList.toggle(class_name, cond);
                        return receiver;
                    };
                } else if (command === 'replace') {
                    return (class_to_replace, class_replace_with) => {
                        target.classList.replace(class_to_replace, class_replace_with);
                        return receiver;
                    };
                }

                throw new Exception('Unexpected classList command: ' + command);
            }

            if (prop === '$id') {
                return (id) => {
                    target.id = id;
                    return receiver;
                }
            }

            if (prop === '$style') {
                return (name, value) => {
                    target.style[name] = value;
                    return receiver;
                };
            }

            const data_prefix = '$data';
            if (prop.startsWith(data_prefix)) {
                const command = prop.slice(data_prefix.length+1);
                return (name, value) => {
                    if (command === '' || command === 'set') {
                        target.dataset[name] = value;
                    } else if (command === 'unset') {
                        delete target.dataset[name];
                    }
                    return receiver;
                };
            }

            if (prop === '$by_selector' || prop === '$find') {
                return (selector) => {
                    return by_selector(selector, target);
                };
            }

            if (prop === '$by_selector_all' || prop === '$find_all') {
                return (selector) => {
                    return by_selector_all(selector, target);
                };
            }

            if (prop === '$append') {
                return (...children) => {
                    for (const child of children) {
                        target.appendChild(get_dom_element(child));
                    }
                    return receiver;
                };
            }

            if (prop === '$prepend') {
                return (...children) => {
                    for (const child of children) {
                        target.prepend(get_dom_element(child));
                    }
                    return receiver;
                };
            }

            if (prop === '$replace') {
                return (node, child) => {
                    target.replaceChild(get_dom_element(node), get_dom_element(child));
                    return receiver;
                };
            }

            if (prop === '$replace_all') {
                return (...children) => {
                    target.replaceChildren(...children.map(get_dom_element));
                    return receiver;
                };
            }

            if (prop === '$clear') {
                return () => {
                    target.replaceChildren();
                    return receiver;
                };
            }

            throw new Exception('Unexpected member: ' + prop);
        },
    };

    function from_dom(element) {
        if (!element) return null;
        if (element.$node) return element;
        return new Proxy(element, tag_handler);
    }

    function get_dom_element(obj) {
        if (typeof(obj) === 'string') {
            return document.createTextNode(obj);
        } else if (typeof(obj) === 'function') {
            return get_dom_element(obj());
        } else if (obj.$node) {
            return obj.$node;
        } else {
            return obj;
        }
    }

    function by_id(id) {
        const element = document.getElementById(id);
        return from_dom(element);
    }

    function by_selector(selector, root = document) {
        const element = root.querySelector(selector);
        return from_dom(element);
    }

    function by_selector_all(selector, root = document) {
        const elements = root.querySelectorAll(selector);
        return Array.from(elements).map(from_dom);
    }

    function img(src, alt) {
        if (!alt) alt = '';
        return tag('img').$attr('src', src).$attr('alt', alt);
    }

    function input(type) {
        return tag('input').$attr('type', type);
    }

    class State {
        #val = '';
        #subs = [];
        #binds = [];
        #name = '';
        auto_notify = true;
        auto_bind = true;
        update_callback = null;
        after_callback = null;
        before_callback = null;

        constructor(initial_state, name, { auto_notify = true, update_callback = null, after_callback = null, before_callback = null, auto_bind = true } = {}) {
            this.#val = initial_state;
            if (update_callback) {
                this.update_callback = update_callback;
            } else {
                this.update_callback = (el, state) => el.textContent = state.toString();
            }
            this.after_callback = after_callback;
            this.before_callback = before_callback;
            this.#name = name;
            this.auto_notify = auto_notify;
            this.auto_bind = auto_bind;
            this.resubscribe_all();
            if (this.auto_notify) this.notify();
        }

        set val(state) {
            if (state !== this.#val) {
                this.#val = state;
                if (this.auto_notify) this.notify();
            }
        }

        get val() {
            return this.#val;
        }

        get name() {
            return this.#name;
        }

        subscribe(element, update_callback = null) {
            if (element.dataset.state === this.#name) this.subscribe1(element);
            const children = element.querySelectorAll(`[data-state="${this.#name}"]`);
            for (const child of children) {
                this.subscribe1(child, { update_callback: update_callback });
            }
        }

        subscribe1(element, { update_callback = null, additional = {} } = {}) {
            this.#subs.push({
                element: from_dom(element),
                update: update_callback,
                ...additional,
            });
        }

        unsubscribe(element) {
            const index = this.#subs.findIndex((val) => val.element === element);
            this.#subs.splice(index, 1);
        }

        input_event_func(prop = 'value') {
            return (e) => {
                if (e.target.type === 'number') {
                    this.val = Number(e.target[prop]);
                } else {
                    this.val = e.target[prop];
                }
            };
        }

        unsubscribe_all() {
            this.#subs = [];
        }

        resubscribe_all(parent = document) {
            this.#subs = [];
            let new_subs_els = parent.querySelectorAll(`[data-state="${this.#name}"]`);
            for (const el of new_subs_els) {
                this.subscribe(el);
            }
        }

        bind_all(parent = document) {
            const elements = parent.querySelectorAll(`input[data-bind="${this.#name}"], input[data-bind^="${this.#name}:"], input[data-bind^="${this.#name}."]`);
            for (const el of elements) {
                this.bind(el);
            }
        }

        unbind_all() {
            // TODO: memory leak
            // for (const bind of this.#binds) {
            //     for (const e of bind.bind_events) {
            //         bind.removeEventListener(e, this.input_event_func(bind.prop));
            //     }
            // }
            this.#binds = [];
        }

        bind(element, { update_callback = null } = {}) {
            let events = null;
            if (element.dataset.bind.includes(':')) {
                events = element.dataset.bind
                    .split(':')[1]
                    .trim()
                    .split(' ')
                    .filter(s => s !== '')
                    .map(s => s.trim());
            } else {
                events = ['input'];
            }
            let prop = 'value';
            if (element.dataset.bind.includes('.')) {
                prop = element.dataset.bind.split(':')[0].split('.')[1];
            }
            for (const e of events) {
                element.addEventListener(e, this.input_event_func(prop));
            }
            if (!update_callback) update_callback = (el, state) => { el[prop] = state }
            this.#binds.push({
                element: element,
                update: update_callback,
                bind_events: events,
                prop: prop,
            });
        }

        notify() {
            if (this.before_callback) this.before_callback(this.#val);
            for (const bind of this.#binds) {
                bind.update(bind.element, this.#val);
            }
            for (const sub of this.#subs) {
                if (sub.update) {
                    sub.update(sub.element, this.#val, sub.additional);
                } else {
                    this.update_callback(sub.element, this.#val);
                }
            }
            if (this.after_callback) this.after_callback(this.#val);
        }
    }
    ultra.State = State;

    const PREDEFINED_TAGS = [
        'div', 'span',
        'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'blockquote', 'pre',
        'a', 'abbr', 'acronym', 'address', 'big', 'cite', 'code',
        'del', 'dfn', 'em', 'ins', 'kbd', 'q', 's', 'samp',
        'small', 'strike', 'strong', 'sub', 'sup', 'tt', 'var',
        'b', 'u', 'i', 'center',
        'dl', 'dt', 'dd', 'ol', 'ul', 'li',
        'table', 'caption', 'tbody', 'tfoot', 'thead', 'tr', 'th', 'td',
        'article', 'aside', 'canvas', 'details', 'embed',
        'figure', 'figcaption', 'footer', 'header', 'hgroup',
        'menu', 'nav', 'output', 'ruby', 'section', 'summary',
        'time', 'mark', 'audio', 'video',
        'button',
    ];
    function add_tags(object) {
        for (const tag_name of PREDEFINED_TAGS) {
            object[tag_name] = (...children) => tag(tag_name, ...children);
        }
        object.img = img;
        object.input = input;
    }

    add_tags(ultra.tags);

    function dump_tags_to_window() {
        add_tags(window);
    }

    return ultra;
})();
