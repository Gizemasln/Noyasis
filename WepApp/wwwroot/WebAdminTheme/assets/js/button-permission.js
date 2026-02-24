// Kullanıcı tipini al
getCurrentUserType() {
    // 1. ÖNCELİK: window.__CURRENT_USER_TYPE (ViewBag'den gelen)
    if (typeof window.__CURRENT_USER_TYPE !== 'undefined' && window.__CURRENT_USER_TYPE) {
        if (window.__CURRENT_USER_TYPE !== 'YOK') {
            this.log('Kullanıcı tipi window.__CURRENT_USER_TYPE\'dan alındı:', window.__CURRENT_USER_TYPE);
            return window.__CURRENT_USER_TYPE;
        }
    }

    // 2. window.userType (geriye uyumluluk)
    if (typeof window.userType !== 'undefined' && window.userType) {
        this.log('Kullanıcı tipi window.userType\'dan alındı:', window.userType);
        return window.userType;
    }

    // 3. Cookie'den almayı dene
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        const [name, value] = cookie.trim().split('=');
        if (name === 'UserType' || name === 'KullaniciTipi' || name === 'kullaniciTipi') {
            const userType = decodeURIComponent(value);
            this.log('Kullanıcı tipi cookie\'den alındı:', userType);
            return userType;
        }
    }

    // 4. localStorage'dan al
    const userType = localStorage.getItem('userType') ||
        localStorage.getItem('KullaniciTipi') ||
        localStorage.getItem('kullaniciTipi');

    if (userType) {
        this.log('Kullanıcı tipi localStorage\'dan alındı:', userType);
        return userType;
    }

    // 5. URL'den almayı dene
    const urlParams = new URLSearchParams(window.location.search);
    const urlUserType = urlParams.get('kullaniciTipi') || urlParams.get('userType');
    if (urlUserType) {
        this.log('Kullanıcı tipi URL\'den alındı:', urlUserType);
        return urlUserType;
    }

    // 6. Sayfa URL'inden çıkarım yap (opsiyonel)
    if (window.location.pathname.includes('/Admin/') ||
        window.location.pathname.includes('Admin')) {
        this.log('URL\'den Admin olduğu çıkarıldı');
        return 'Admin';
    }

    // 7. Varsayılan olarak Admin (Musteri DEĞİL!)
    this.log('Varsayılan kullanıcı tipi kullanılıyor: Admin');
    return 'Admin';
}