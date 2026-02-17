/**
 * Buton Yetkilendirme Sistemi
 * Layout'a eklenecek script
 * Tüm sayfalarda butonları otomatik gizler/gösterir
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

    // Kullanıcı tipini al (cookie'den veya localStorage'dan)
    getCurrentUserType() {
        // Cookie'den almayı dene
        const cookies = document.cookie.split(';');
        for (let cookie of cookies) {
            const [name, value] = cookie.trim().split('=');
            if (name === 'UserType' || name === 'KullaniciTipi') {
                return value;
            }
        }

        // localStorage'dan al
        const userType = localStorage.getItem('userType') || localStorage.getItem('KullaniciTipi');
        if (userType) return userType;

        // Varsayılan olarak Admin (geliştirme aşamasında)
        return 'Admin';
    }

    // Mevcut sayfa adını al (controller)
    getCurrentPageName() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(segment => segment);

        if (segments.length > 0) {
            // Controller adını al (ilk segment)
            let controller = segments[0];
            // Controller kelimesini temizle
            controller = controller.replace('Controller', '');
            return controller;
        }
        return 'Home';
    }

    // İzinleri backend'den yükle
    async loadPermissions() {
        try {
            const response = await fetch('/AdminButton/TumIzinleriGetirJson');

            if (response.ok) {
                const data = await response.json();
                this.permissions = this.formatPermissions(data);
                this.initialized = true;
                this.retryCount = 0;
                console.log('Buton izinleri yüklendi');
                this.applyPermissions();
            } else {
                console.error('İzinler yüklenemedi:', response.status);
                if (this.retryCount < this.maxRetry) {
                    this.retryCount++;
                    setTimeout(() => this.loadPermissions(), 1000);
                }
            }
        } catch (error) {
            console.error('İzinler yüklenirken hata:', error);
            if (this.retryCount < this.maxRetry) {
                this.retryCount++;
                setTimeout(() => this.loadPermissions(), 1000);
            }
        }
    }

    // Gelen veriyi formatla
    formatPermissions(data) {
        const formatted = {};

        // Her kullanıcı tipi için
        for (const userType in data) {
            formatted[userType] = {};

            // Her buton izni için
            if (Array.isArray(data[userType])) {
                data[userType].forEach(permission => {
                    const key = `${permission.sayfaAdi}|${permission.butonAksiyonu}`;
                    formatted[userType][key] = permission.izınVar;
                });
            }
        }

        return formatted;
    }

    // Belirli bir buton için izin kontrolü
    hasPermission(buttonAction, customPage = null) {
        if (!this.initialized) {
            return true; // Henüz yüklenmediyse göster
        }

        const page = customPage || this.currentPage;
        const key = `${page}|${buttonAction}`;

        // Admin her şeyi görebilir (isteğe bağlı)
        if (this.currentUserType === 'Admin') {
            return true;
        }

        // İzin kontrolü
        if (this.permissions[this.currentUserType] &&
            this.permissions[this.currentUserType][key] !== undefined) {
            return this.permissions[this.currentUserType][key];
        }

        // Varsayılan olarak false (yetki yok)
        return false;
    }

    // Sayfadaki tüm butonları kontrol et
    applyPermissions() {
        if (!this.initialized) {
            console.log('İzinler yükleniyor, butonlar henüz kontrol edilmedi');
            return;
        }

        console.log('Buton izinleri uygulanıyor...');

        // 1. data-permission attribute'u ile tanımlanan butonlar
        document.querySelectorAll('[data-permission]').forEach(element => {
            this.processPermissionElement(element);
        });

        // 2. data-button-permission attribute'u ile tanımlanan butonlar
        document.querySelectorAll('[data-button-permission]').forEach(element => {
            this.processPermissionElement(element);
        });

        // 3. class ile tanımlanan butonlar (isteğe bağlı)
        this.processCommonButtons();
    }

    // Tek bir permission elementini işle
    processPermissionElement(element) {
        const permissionAttr = element.getAttribute('data-permission') ||
            element.getAttribute('data-button-permission');

        if (!permissionAttr) return;

        // Format: "Controller|Action" veya sadece "Action"
        const parts = permissionAttr.split('|');
        let action, page;

        if (parts.length === 2) {
            page = parts[0];
            action = parts[1];
        } else {
            action = permissionAttr;
            page = this.currentPage;
        }

        const hasPermission = this.hasPermission(action, page);

        if (!hasPermission) {
            this.hideElement(element);
        }
    }

    // Yaygın butonları işle (icon bazlı)
    processCommonButtons() {
        // Ekle butonları (fa-plus)
        document.querySelectorAll('.btn-primary, .btn-success, a.btn-primary, a.btn-success').forEach(btn => {
            if (btn.innerHTML.includes('fa-plus') || btn.textContent.includes('Ekle') || btn.textContent.includes('Yeni')) {
                if (!this.hasPermission('create')) {
                    this.hideElement(btn);
                }
            }
        });

        // Düzenle butonları (fa-edit)
        document.querySelectorAll('.btn-warning, .btn-edit, a.btn-warning').forEach(btn => {
            if (btn.innerHTML.includes('fa-edit') || btn.textContent.includes('Düzenle') || btn.textContent.includes('Güncelle')) {
                if (!this.hasPermission('edit')) {
                    this.hideElement(btn);
                }
            }
        });

        // Sil butonları (fa-trash)
        document.querySelectorAll('.btn-danger, .btn-delete, a.btn-danger').forEach(btn => {
            if (btn.innerHTML.includes('fa-trash') || btn.textContent.includes('Sil')) {
                if (!this.hasPermission('delete')) {
                    this.hideElement(btn);
                }
            }
        });

        // Detay butonları (fa-eye)
        document.querySelectorAll('.btn-info, .btn-view, .btn-detail, a.btn-info').forEach(btn => {
            if (btn.innerHTML.includes('fa-eye') || btn.textContent.includes('Detay') || btn.textContent.includes('Görüntüle')) {
                if (!this.hasPermission('view')) {
                    this.hideElement(btn);
                }
            }
        });

        // Tablo içindeki işlem butonları
        document.querySelectorAll('td:last-child button, td:last-child a').forEach(btn => {
            const btnHtml = btn.outerHTML.toLowerCase();

            if (btnHtml.includes('fa-edit') || btnHtml.includes('düzenle') || btnHtml.includes('duzenle')) {
                if (!this.hasPermission('edit')) {
                    this.hideElement(btn);
                }
            }
            else if (btnHtml.includes('fa-trash') || btnHtml.includes('sil')) {
                if (!this.hasPermission('delete')) {
                    this.hideElement(btn);
                }
            }
            else if (btnHtml.includes('fa-eye') || btnHtml.includes('detay') || btnHtml.includes('görüntüle')) {
                if (!this.hasPermission('view')) {
                    this.hideElement(btn);
                }
            }
        });
    }

    // Elementi gizle
    hideElement(element) {
        element.style.display = 'none';
    }

    // Elementi devre dışı bırak (isteğe bağlı)
    disableElement(element) {
        element.disabled = true;
        element.style.opacity = '0.5';
        element.style.cursor = 'not-allowed';
    }

    // Sayfa yüklendiğinde başlat
    initialize() {
        this.loadPermissions();
        this.observeDynamicContent();
    }

    // Dinamik içerik (AJAX, modal vb.) için observer
    observeDynamicContent() {
        const observer = new MutationObserver((mutations) => {
            let shouldRefresh = false;

            mutations.forEach((mutation) => {
                if (mutation.addedNodes.length > 0) {
                    shouldRefresh = true;
                }
            });

            if (shouldRefresh && this.initialized) {
                setTimeout(() => this.applyPermissions(), 100);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Manuel olarak yenile
    refresh() {
        this.currentPage = this.getCurrentPageName();
        this.applyPermissions();
    }
}

// Global instance oluştur
window.buttonPermissionManager = new ButtonPermissionManager();

// Sayfa yüklendiğinde başlat
document.addEventListener('DOMContentLoaded', function () {
    window.buttonPermissionManager.initialize();
});

// Sayfa değişikliklerini dinle (SPA için)
window.addEventListener('popstate', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

// Tab değişimlerini dinle (Bootstrap modallar, tablar)
document.addEventListener('shown.bs.tab', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

document.addEventListener('shown.bs.modal', function () {
    setTimeout(() => {
        window.buttonPermissionManager.refresh();
    }, 300);
});

// AJAX çağrılarından sonra (jQuery varsa)
if (typeof $ !== 'undefined') {
    $(document).ajaxComplete(function () {
        setTimeout(() => {
            window.buttonPermissionManager.refresh();
        }, 300);
    });
}