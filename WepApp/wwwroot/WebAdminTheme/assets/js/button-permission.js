/**
* Buton Yetkilendirme Sistemi
* Tüm sayfalarda butonları otomatik gizler/gösterir
*/

class ButtonPermissionManager {
    constructor() {
        this.permissions = {};
        this.currentUserType = this.getCurrentUserType();
        this.currentPage = this.getCurrentPageName();
        this.initialized = false;

    }

    // Kullanıcı tipini al
    getCurrentUserType() {
        // ViewBag'den gelen global değişken
        if (window.__CURRENT_USER_TYPE && window.__CURRENT_USER_TYPE !== 'YOK') {
            return window.__CURRENT_USER_TYPE;
        }
        return 'Admin';
    }

    // Mevcut sayfa adını al
    getCurrentPageName() {
        const path = window.location.pathname;
        const segments = path.split('/').filter(segment => segment);

        if (segments.length > 0) {
            let controller = segments[0];
            // Controller varsa temizle
            controller = controller.replace('Controller', '');
            return controller;
        }
        return 'Home';
    }

    // İzinleri backend'den yükle
    async loadPermissions() {
        try {

            const response = await fetch('/AdminButton/TumIzinleriGetirJson', {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });

            if (response.ok) {
                const data = await response.json();

                this.permissions = data;
                this.initialized = true;
                this.applyPermissions();
            } else {
            }
        } catch (error) {
        }
    }

    // Belirli bir buton için izin kontrolü
    hasPermission(buttonAction) {
        if (!this.initialized) {
            return true; // Henüz yüklenmediyse göster
        }

        // Anahtar formatı: "SayfaAdi|ButonAksiyonu"
        const key = `${this.currentPage}|${buttonAction}`;

        if (this.permissions[this.currentUserType] &&
            this.permissions[this.currentUserType][key] !== undefined) {

            const result = this.permissions[this.currentUserType][key];
            return result;
        }

        return false;
    }

    // Sayfadaki tüm butonları kontrol et
    applyPermissions() {
        if (!this.initialized) return;

  

        // 1. data-button-permission attribute'u olanları kontrol et
        document.querySelectorAll('[data-button-permission]').forEach(element => {
            const permission = element.getAttribute('data-button-permission');
            const hasPermission = this.hasPermission(permission);

            if (!hasPermission) {
                element.style.display = 'none';
                element.classList.add('d-none');
            }
        });

        // 2. data-permission attribute'u olanları kontrol et
        document.querySelectorAll('[data-permission]').forEach(element => {
            const permission = element.getAttribute('data-permission');
            const hasPermission = this.hasPermission(permission);

            if (!hasPermission) {
                element.style.display = 'none';
                element.classList.add('d-none');
            }
        });

    }

    // Manuel yenileme
    refresh() {
        this.currentPage = this.getCurrentPageName();
        this.currentUserType = this.getCurrentUserType();

        if (this.initialized) {
            this.applyPermissions();
        } else {
            this.loadPermissions();
        }
    }

    // Dinamik içerik için observer
    observeDynamicContent() {
        const observer = new MutationObserver((mutations) => {
            let shouldRefresh = false;

            mutations.forEach((mutation) => {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
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

    // Başlat
    initialize() {
        this.loadPermissions();
        this.observeDynamicContent();

        // Sayfa tamamen yüklendiğinde tekrar kontrol et
        window.addEventListener('load', () => {
            setTimeout(() => this.refresh(), 500);
        });
    }
}

// Global instance oluştur
window.buttonPermissionManager = new ButtonPermissionManager();

// DOM yüklendiğinde başlat
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.buttonPermissionManager.initialize();
    });
} else {
    window.buttonPermissionManager.initialize();
}

// Sayfa değişikliklerini dinle
window.addEventListener('popstate', () => {
    setTimeout(() => window.buttonPermissionManager.refresh(), 300);
});

// Bootstrap modal eventleri
document.addEventListener('shown.bs.modal', () => {
    setTimeout(() => window.buttonPermissionManager.refresh(), 300);
});
