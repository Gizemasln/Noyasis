import random
import string
import hashlib

def generate_simple_key(username=""):
    """Kullanıcı adına dayalı basit anahtar üretimi"""
    if username:
        # Kullanıcı adının hash'ini al
        hash_obj = hashlib.md5(username.encode())
        hash_digest = hash_obj.hexdigest().upper()
        
        # Formatla: XXXX-XXXX-XXXX-XXXX
        key = '-'.join([hash_digest[i:i+4] for i in range(0, 16, 4)])
        return key[:19]  # 16 karakter + 3 tire
    else:
        # Rastgele anahtar
        chars = string.ascii_uppercase + string.digits
        segments = [''.join(random.choices(chars, k=4)) for _ in range(4)]
        return '-'.join(segments)

# Kullanım
print(generate_simple_key("testuser"))
print(generate_simple_key())