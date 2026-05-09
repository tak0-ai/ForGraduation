export function execCommand(editorId, command, value = null) {
    const element = document.getElementById(editorId);
    if (!element) return;
    element.focus();
    document.execCommand(command, false, value);
}

export function insertHtml(editorId, html) {
    const element = document.getElementById(editorId);
    if (!element) return;
    element.focus();
    document.execCommand('insertHTML', false, html);
}

export function setHtml(editorId, html) {
    const element = document.getElementById(editorId);
    if (!element) return;
    element.innerHTML = html;
}

export function getHtml(editorId) {
    const element = document.getElementById(editorId);
    return element ? element.innerHTML : '';
}

export function initEditor(editorId) {
    const element = document.getElementById(editorId);
    if (!element) return;

    element.addEventListener('paste', function (e) {
        const clipboardData = e.clipboardData || window.clipboardData;
        if (!clipboardData) return;

        const types = clipboardData.types ? Array.from(clipboardData.types) : [];
        const items = clipboardData.items;
        
        // 1. 处理富文本粘贴（解决从 Word 或其他 App 粘贴时图片链接失效的问题）
        if (types.includes('text/html')) {
            const imageItems = Array.from(items).filter(item => item.type.indexOf('image/') !== -1);
            if (imageItems.length > 0) {
                e.preventDefault();
                const html = clipboardData.getData('text/html');
                const container = document.createElement('div');
                container.innerHTML = html;
                const imgElements = container.querySelectorAll('img');
                
                const promises = [];
                let itemIndex = 0;

                for (let i = 0; i < imgElements.length && itemIndex < imageItems.length; i++) {
                    const img = imgElements[i];
                    const src = img.getAttribute('src') || '';
                    
                    // 识别需要替换的无效或本地链接：file://, cid:, blob: (跨域可能无效), 或者空链接
                    if (!src || src.startsWith('file:') || src.startsWith('cid:') || src.startsWith('blob:') || src.startsWith('webkit-fake-url:')) {
                        const file = imageItems[itemIndex++].getAsFile();
                        if (file) {
                            promises.push(new Promise(resolve => {
                                const reader = new FileReader();
                                reader.onload = (event) => {
                                    img.setAttribute('src', event.target.result);
                                    resolve();
                                };
                                reader.readAsDataURL(file);
                            }));
                        }
                    }
                }

                if (promises.length > 0) {
                    Promise.all(promises).then(() => {
                        insertHtml(editorId, container.innerHTML);
                    });
                } else {
                    insertHtml(editorId, html);
                }
                return;
            }
            // 如果只是纯文本/HTML 而没有图片附件，则让浏览器原生处理以保持最高兼容性
            return;
        }

        // 2. 处理纯图片粘贴（如截图）
        let imageFiles = [];
        for (const item of items) {
            if (item.type.indexOf('image/') !== -1) {
                const file = item.getAsFile();
                if (file) imageFiles.push(file);
            }
        }
        
        if (imageFiles.length > 0) {
            e.preventDefault();
            const promises = imageFiles.map(file => new Promise(resolve => {
                const reader = new FileReader();
                reader.onload = (event) => resolve(`<p><img src="${event.target.result}" style="max-width:100%;" /></p>`);
                reader.readAsDataURL(file);
            }));
            
            Promise.all(promises).then(htmls => {
                insertHtml(editorId, htmls.join(''));
            });
        }
    });
}

export function createLink(editorId, url) {
    const element = document.getElementById(editorId);
    if (!element) return;
    element.focus();
    
    document.execCommand('createLink', false, url);
    
    const links = element.getElementsByTagName('a');
    for (let i = 0; i < links.length; i++) {
        if (links[i].getAttribute('href') === url && !links[i].hasAttribute('target')) {
            links[i].setAttribute('target', '_blank');
        }
    }
}

export async function getBlobImages(editorId) {
    const element = document.getElementById(editorId);
    if (!element) return [];
    const imgs = Array.from(element.querySelectorAll('img'));
    const results = [];
    for (const img of imgs) {
        const src = img.getAttribute('src');
        if (!src) continue;
        if (src.startsWith('blob:')) {
            try {
                const resp = await fetch(src);
                const blob = await resp.blob();
                const reader = new FileReader();
                const dataUrl = await new Promise((resolve, reject) => {
                    reader.onerror = reject;
                    reader.onload = () => resolve(reader.result);
                    reader.readAsDataURL(blob);
                });
                // supply original src and dataUrl and blob type
                results.push({ src, dataUrl, type: blob.type });
            }
            catch (e) {
                // ignore failed blobs
            }
        } else if (src.startsWith('data:image')) {
            results.push({ src, dataUrl: src });
        }
    }
    return results;
}



