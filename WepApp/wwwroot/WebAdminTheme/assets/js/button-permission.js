/**
 * Buton Yetkilendirme Sistemi
 * Layout'ta dahil edilecek
 */

class ButtonPermissionManager {
    constructor() {
        this.permissions = {};
        this.currentUserType = this.getCurrentUserType();
        this.currentPage = this.getCurrentPageName();
        this.initialized = false;
        this.retryCount = 0;
        this.maxRetry = 3;
    }

    // Kullanıcı tipini al
    getCurrentUserType() {
        // URL'den veya cookie'den al
        var urlParams = new URLSearchParams(window.location.search);
        var tip = urlParams.get('kullaniciTipi');
        if (tip) return tip;

        // LocalStorage'dan al
        return localStorage.getItem('userType') || 'Admin';
    }

    // Mevcut sayfa adını al
    getCurrentPageName() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(segment => segment);
        if (segments.length > 0) {
            // Controller adını al (ilk segment)
            return segments[0].replace('Controller', '');
        }
        return 'Home';
    }

    // İzinleri backend'den yükle - 404 hatası düzeltmesi
    async loadPermissions() {
        try {
            // Doğru endpoint: AdminButtonController/TumIzinleriGetirJson
            const response = await fetch('/AdminButton/TumIzinleriGetirJson');

            if (response.ok) {
                this.permissions = await response.json();
                this.initialized = true;
                this.retryCount = 0;
                console.log('Buton izinleri yüklendi:', this.permissions);
                this.applyPermissions();
            } else {
                console.error('İzinler yüklenemedi. Status:', response.status);
                if (this.retryCount < this.maxRetry) {
                    this.retryCount++;
                    console.log(`Yeniden deneniyor (${this.retryCount}/${this.maxRetry})...`);
                    setTimeout(() => this.loadPermissions(), 1000);
                } else {
                    this.setDefaultPermissions();
                }
            }
        } catch (error) {
            console.error('İzinler yüklenirken hata:', error);
            if (this.retryCount < this.maxRetry) {
                this.retryCount++;
                setTimeout(() => this.loadPermissions(), 1000);
            } else {
                this.setDefaultPermissions();
            }
        }
    }

    // Varsayılan izinler
    setDefaultPermissions() {
        console.log('Varsayılan izinler kullanılıyor (Admin tam yetkili)');
        this.permissions = {
            'Admin': {},
            'Musteri': {},
            'Bayi': {},
            'Distributor': {}
        };
        this.initialized = true;
        this.applyPermissions();
    }

    // Belirli bir buton için izin kontrolü
    hasPermission(buttonAction) {
        if (!this.initialized) {
            return true; // Henüz yüklenmediyse göster
        }

        const key = `${this.currentPage}|${buttonAction}`;

        if (this.permissions[this.currentUserType] &&
            this.permissions[this.currentUserType][key] !== undefined) {
            return this.permissions[this.currentUserType][key];
        }

        return this.currentUserType === 'Admin'; // Admin her şeyi görebilir
    }

    // Sayfadaki tüm butonları kontrol et
    applyPermissions() {
        if (!this.initialized) {
            console.log('İzinler yükleniyor...');
            return;
        }

        console.log('Buton izinleri uygulanıyor...');

        // 1. Class ile tanımlanan butonlar
        document.querySelectorAll('.btn-permission').forEach(button => {
            this.processButton(button);
        });

        // 2. Data attribute ile tanımlanan butonlar
        document.querySelectorAll('[data-button-permission]').forEach(button => {
            this.processButton(button);
        });

        // 3. Özel buton sınıfları
        this.processIconButtons();
    }

    // Tek bir butonu işle
    processButton(button) {
        const buttonAction = button.getAttribute('data-action') ||
            button.getAttribute('data-button-permission') ||
            this.detectButtonAction(button);

        const pageName = button.getAttribute('data-page') || this.currentPage;

        if (!buttonAction) return;

        const key = `${pageName}|${buttonAction}`;
        let hasPermission = this.currentUserType === 'Admin'; // Admin her şeyi görebilir

        if (this.permissions[this.currentUserType] &&
            this.permissions[this.currentUserType][key] !== undefined) {
            hasPermission = this.permissions[this.currentUserType][key];
        }

        if (!hasPermission) {
            this.hideOrDisableButton(button);
        }
    }

    // Icon butonlarını işle
    processIconButtons() {
        const iconSelectors = [
            'a.fa-edit, button.fa-edit, i.fa-edit',
            'a.fa-trash, button.fa-trash, i.fa-trash',
            'a.fa-eye, button.fa-eye, i.fa-eye',
            'a.fa-plus, button.fa-plus, i.fa-plus',
            'a.fa-download, button.fa-download, i.fa-download',
            'a.fa-print, button.fa-print, i.fa-print',
            'a.fa-save, button.fa-save, i.fa-save',
            'a.fa-check, button.fa-check, i.fa-check',
            'a.fa-times, button.fa-times, i.fa-times'
        ];

        iconSelectors.forEach(selector => {
            document.querySelectorAll(selector).forEach(icon => {
                const buttonAction = this.detectIconAction(icon);
                if (buttonAction) {
                    const button = this.findParentButton(icon) || icon;
                    button.setAttribute('data-button-permission', buttonAction);
                    this.processButton(button);
                }
            });
        });
    }

    // Icon'a göre aksiyonu belirle
    detectIconAction(icon) {
        const classList = icon.className;

        if (classList.includes('fa-edit') || classList.includes('fa-pencil')) return 'edit';
        if (classList.includes('fa-trash') || classList.includes('fa-times')) return 'delete';
        if (classList.includes('fa-eye') || classList.includes('fa-search')) return 'view';
        if (classList.includes('fa-plus') || classList.includes('fa-add')) return 'create';
        if (classList.includes('fa-download')) return 'download';
        if (classList.includes('fa-print')) return 'print';
        if (classList.includes('fa-file-export')) return 'export';
        if (classList.includes('fa-file-import')) return 'import';
        if (classList.includes('fa-save')) return 'save';
        if (classList.includes('fa-check')) return 'approve';

        return null;
    }

    // Butonun parent'ını bul
    findParentButton(element) {
        let current = element;
        while (current && current !== document.body) {
            if (current.tagName === 'BUTTON' || current.tagName === 'A') {
                return current;
            }
            current = current.parentElement;
        }
        return null;
    }

    // Buton aksiyonunu otomatik belirle
    detectButtonAction(button) {
        const text = button.textContent.toLowerCase();
        const classList = button.className.toLowerCase();

        if (text.includes('sil') || classList.includes('delete')) return 'delete';
        if (text.includes('düzenle') || text.includes('duzenle') || text.includes('edit')) return 'edit';
        if (text.includes('ekle') || text.includes('yeni') || text.includes('create')) return 'create';
        if (text.includes('görüntüle') || text.includes('goruntule') || text.includes('view') || text.includes('detay')) return 'view';
        if (text.includes('indir') || text.includes('download')) return 'download';
        if (text.includes('yazdır') || text.includes('print')) return 'print';
        if (text.includes('kaydet') || text.includes('save')) return 'save';
        if (text.includes('onayla') || text.includes('approve')) return 'approve';
        if (text.includes('reddet') || text.includes('reject')) return 'reject';

        return null;
    }

    // Butonu gizle
    hideOrDisableButton(button) {
        button.style.display = 'none';
    }

    // Sayfa yüklendiğinde çalıştır
    initialize() {
        this.loadPermissions();
        this.observeDynamicContent();
    }

    // Dinamik içerik için observer
    observeDynamicContent() {
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.addedNodes.length) {
                    setTimeout(() => {
                        this.applyPermissions();
                    }, 100);
                }
            });
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Manuel olarak izinleri yeniden uygula
    refresh() {
        this.currentPage = this.getCurrentPageName();
        this.applyPermissions();
    }
}

// Global instance
window.buttonPermissionManager = new ButtonPermissionManager();

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', function () {
    window.buttonPermissionManager.initialize();
});

// Tab değişimlerini dinle
document.addEventListener('shown.bs.tab', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

// AJAX çağrılarından sonra
if (typeof $ !== 'undefined') {
    $(document).ajaxComplete(function () {
        setTimeout(() => {
            window.buttonPermissionManager.refresh();
        }, 300);
    });
}