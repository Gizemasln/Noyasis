using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WepApp.Models;

namespace WebApp.Models;

public partial class Context : DbContext
{
    public Context()
    {
    }

    public Context(DbContextOptions<Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AnaSayfaFotograf> AnaSayfaFotograf { get; set; }
    public virtual DbSet<AnaSayfaRakamlari> AnaSayfaRakamlari { get; set; }
    public virtual DbSet<HakkimizdaBilgileri> HakkimizdaBilgileri { get; set; }
    public virtual DbSet<Nedenler> Nedenler { get; set; }
    public virtual DbSet<UYB> UYB { get; set; }
    public virtual DbSet<Slider> Slider { get; set; }
    public virtual DbSet<ButtonPermission> ButtonPermissions { get; set; }
    public virtual DbSet<MenuIzin> MenuIzin { get; set; }

    public virtual DbSet<IstekOneriDurum> IstekOneriDurum{ get; set; }
    public virtual DbSet<ARGEDurum> ARGEDurum { get; set; }
    public virtual DbSet<ArgeHata> ArgeHata { get; set; }
    public virtual DbSet<PaketBaglanti> PaketBaglanti { get; set; }
    public virtual DbSet<IstekOneriler> IstekOneriler { get; set; }
    public virtual DbSet<NeredenDuydu> NeredenDuydu { get; set; }
    public virtual DbSet<Departman> Departman { get; set; }
    public virtual DbSet<BayiDuyuru> BayiDuyuru { get; set; }
    public virtual DbSet<MusteriYetkililer> MusteriYetkililer { get; set; }
    public virtual DbSet<IletisimBilgileri> IletisimBilgileri { get; set; }
    public virtual DbSet<BayiSozlesme> BayiSozlesme { get; set; }
    public virtual DbSet<MusteriSozlesme> MusteriSozlesme { get; set; }
    public virtual DbSet<BayiSozlesmeBayiKriter> BayiSozlesmeBayiKriter { get; set; }
    public virtual DbSet<KVKK> KVKK { get; set; }
    public virtual DbSet<KDV> KDV { get; set; }
    public virtual DbSet<Entegrator> Entegrator { get; set; }
    public virtual DbSet<BayiSertifika> BayiSertifika { get; set; }
    public virtual DbSet<BayiSozlesmeKriteri> BayiSozlesmeKriteri { get; set; }
    public virtual DbSet<SozlesmeDurumu> SozlesmeDurumu { get; set; }
    public virtual DbSet<LisansDurumu> LisansDurumu { get; set; }
    public virtual DbSet<BayiYetkililer> BayiYetkililer { get; set; }
    public virtual DbSet<Sayac> Sayac { get; set; }
    public virtual DbSet<Kampanya> Kampanya { get; set; }
    public virtual DbSet<KampanyaPaket> KampanyaPaket { get; set; }
    public virtual DbSet<FiyatOran> FiyatOran { get; set; }
    public virtual DbSet<TeklifDetay> TeklifDetay { get; set; }
    public virtual DbSet<Makale> Makale { get; set; }
    public virtual DbSet<SSS> SSS { get; set; }
    public virtual DbSet<Teklif> Teklif { get; set; }
    public virtual DbSet<Yetki> Yetki { get; set; }
    public virtual DbSet<MusteriTipi> MusteriTipi { get; set; }
    public virtual DbSet<TeklifDurum> TeklifDurum { get; set; }
    public virtual DbSet<Birim> Birim { get; set; }
    public virtual DbSet<PaketGrupDetay> PaketGrupDetay { get; set; }
    public virtual DbSet<LisansTip> LisansTip { get; set; }
    public virtual DbSet<Paket> Paket { get; set; }
    public virtual DbSet<PaketGrup> PaketGrup { get; set; }
    public virtual DbSet<Duyuru> Duyuru { get; set; }
    public virtual DbSet<Urun> Urun { get; set; }
    public virtual DbSet<Musteri> Musteri { get; set; }
    public virtual DbSet<Bayi> Bayi { get; set; } // Bayi DbSet eklendi
    public virtual DbSet<Lokasyon> Lokasyon { get; set; }
    public virtual DbSet<Kategori> Kategori { get; set; }
    public virtual DbSet<IKFormu> IKFormu { get; set; }
    public virtual DbSet<Teklifler> Teklifler { get; set; }
    public virtual DbSet<GenelAydinlatma> GenelAydinlatma { get; set; }
    public virtual DbSet<Kullanicilar> Kullanicilar { get; set; }
    public virtual DbSet<AnaSayfaBannerResim> AnaSayfaBannerResim { get; set; }
    public virtual DbSet<HakkimizdaFotograf> HakkimizdaFotograf { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(
            "Server=193.35.155.81,1433;Database=Noyasis;User Id=sqluser;Password=5343212901Ga*;TrustServerCertificate=True;");




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
     


        // ==================== AnaSayfaFotograf ====================
        modelBuilder.Entity<AnaSayfaFotograf>(entity =>
        {
            entity.ToTable("AnaSayfaFotograf");
            entity.Property(e => e.EklenmeTarihi).HasColumnType("datetime");
            entity.Property(e => e.GuncellenmeTarihi).HasColumnType("datetime");
        });

        // ==================== AnaSayfaRakamlari ====================
        modelBuilder.Entity<AnaSayfaRakamlari>(entity =>
        {
            entity.ToTable("AnaSayfaRakamlari");
            entity.Property(e => e.EklenmeTarihi).HasColumnType("datetime");
            entity.Property(e => e.GuncellenmeTarihi).HasColumnType("datetime");
        });

        // ==================== HakkimizdaBilgileri ====================
        modelBuilder.Entity<HakkimizdaBilgileri>(entity =>
        {
            entity.ToTable("HakkimizdaBilgileri");
            entity.Property(e => e.EklenmeTarihi).HasColumnType("datetime");
            entity.Property(e => e.GuncellenmeTarihi).HasColumnType("datetime");
        });

        // ==================== IletisimBilgileri ====================
        modelBuilder.Entity<IletisimBilgileri>(entity =>
        {
            entity.ToTable("IletisimBilgileri");
            entity.Property(e => e.BankaAdi).HasMaxLength(50);
            entity.Property(e => e.Email1)
                .HasMaxLength(255)
                .HasColumnName("EMail_1");
            entity.Property(e => e.Email2)
                .HasMaxLength(50)
                .HasColumnName("EMail_2");
            entity.Property(e => e.Faks).HasMaxLength(50);
            entity.Property(e => e.IbanNo).HasMaxLength(50);
            entity.Property(e => e.Telefon1)
                .HasMaxLength(50)
                .HasColumnName("Telefon_1");
            entity.Property(e => e.Telefon2)
                .HasMaxLength(50)
                .HasColumnName("Telefon_2");
            entity.Property(e => e.Telefon3)
                .HasMaxLength(50)
                .HasColumnName("Telefon_3");
            entity.Property(e => e.Telefon4)
                .HasMaxLength(50)
                .HasColumnName("Telefon_4");
            entity.Property(e => e.WhatsApp).HasMaxLength(50);
        });

        // ==================== Kullanicilar ====================
        modelBuilder.Entity<Kullanicilar>(entity =>
        {
            entity.ToTable("Kullanicilar");
            entity.Property(e => e.Adi).HasMaxLength(50);
            entity.Property(e => e.EklenmeTarihi).HasColumnType("datetime");
            entity.Property(e => e.GuncellenmeTarihi).HasColumnType("datetime");
            entity.Property(e => e.Sifre).HasMaxLength(50);

            // Kullanicilar - Yetki ilişkisi
            entity.HasOne(k => k.Yetki)
                .WithOne(y => y.Kullanicilar)
                .HasForeignKey<Kullanicilar>(k => k.YetkiId);
        });

        // ==================== BAYI CONFIGURATION (SONSUZ SEVİYE) ====================
        modelBuilder.Entity<Bayi>(entity =>
        {
            entity.ToTable("Bayi");

            // Primary Key
            entity.HasKey(e => e.Id);

            // Properties
            entity.Property(e => e.Unvan)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.KullaniciAdi)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Sifre)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Email)
                .HasMaxLength(255);

            entity.Property(e => e.Telefon)
                .HasMaxLength(20);

            entity.Property(e => e.Adres)
                .HasMaxLength(500);

            entity.Property(e => e.Seviye)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.Durumu)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.EklenmeTarihi)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.GuncellenmeTarihi)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            // Self-referencing ilişki (Üst Bayi - Alt Bayiler)
            entity.HasOne(b => b.UstBayi)
                .WithMany(b => b.AltBayiler)
                .HasForeignKey(b => b.UstBayiId)
                .OnDelete(DeleteBehavior.Restrict); // Cascade delete engellendi



            // Index'ler (Performans için)
            entity.HasIndex(b => b.UstBayiId)
                .HasDatabaseName("IX_Bayi_UstBayiId");

  

            entity.HasIndex(b => b.KullaniciAdi)
                .IsUnique()
                .HasDatabaseName("IX_Bayi_KullaniciAdi")
                .HasFilter("[Durumu] = 1"); // Sadece aktif bayiler için unique

            entity.HasIndex(b => b.Seviye)
                .HasDatabaseName("IX_Bayi_Seviye");

            entity.HasIndex(b => b.Durumu)
                .HasDatabaseName("IX_Bayi_Durumu");

            // Composite Index
            entity.HasIndex(b => new { b.UstBayiId, b.Durumu })
                .HasDatabaseName("IX_Bayi_UstBayi_Durumu");
        });

        // ==================== MÜŞTERİ CONFIGURATION ====================
        modelBuilder.Entity<Musteri>(entity =>
        {
            entity.ToTable("Musteri");

            // Primary Key
            entity.HasKey(e => e.Id);

            // Properties
            entity.Property(e => e.Ad)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Soyad)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.KullaniciAdi)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Sifre)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Email)
                .HasMaxLength(255);

            entity.Property(e => e.Telefon)
                .HasMaxLength(20);

            entity.Property(e => e.Adres)
                .HasMaxLength(500);

            entity.Property(e => e.Durum)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.EklenmeTarihi)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.GuncellenmeTarihi)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETDATE()");


     
            // Index'ler
            entity.HasIndex(m => m.KullaniciAdi)
                .IsUnique()
                .HasDatabaseName("IX_Musteri_KullaniciAdi")
                .HasFilter("[Durum] = 1");

            entity.HasIndex(m => m.Durum)
                .HasDatabaseName("IX_Musteri_Durum");
        });

        // Base OnModelCreating
        base.OnModelCreating(modelBuilder);

        // Partial method çağrısı
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}