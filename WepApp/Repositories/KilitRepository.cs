using WebApp.Repositories;
using WepApp.Models;

namespace WepApp.Repositories
{
    public class KilitRepository : GenericRepository<Kilit>
    {
        // KilitRepository.cs'ye eklenecek yardımcı metod

        public (bool aktif, int gun) GetKilitAyar()
        {
            var kilit = Getir(x => x.Durumu == 1);
            if (kilit != null)
            {
                return (kilit.Aktif, kilit.Gun);
            }
            // Varsayılan değerler
            return (true, 15);
        }
    }
}
