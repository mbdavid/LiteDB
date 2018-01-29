using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    internal class WalService
    {
        private LockService _locker;
        private FileService _datafile;
        private Logger _log;

        private Dictionary<Guid, HeaderPage> _confirmedTransactions = new Dictionary<Guid, HeaderPage>();
        private ConcurrentDictionary<uint, ConcurrentDictionary<int, long>> _index = new ConcurrentDictionary<uint, ConcurrentDictionary<int, long>>();

        private int _currentReadVersion = 0;

        public WalService(LockService locker, FileService datafile, Logger log)
        {
            _locker = locker;
            _datafile = datafile;
            _log = log;
        }

        /// <summary>
        /// Get current read version for all new transactions
        /// </summary>
        public int CurrentReadVersion => _currentReadVersion;

        /// <summary>
        /// Checks if an Page/Version are in WAL-index memory. Consider version that are below parameter. Returns PagePosition of this page inside WAL-file or Empty if page doesn't found.
        /// </summary>
        public long GetPageIndex(uint pageID, int version)
        {
            // wal-index versions must be greater than 0 (version 0 is datafile)
            if (version == 0) return long.MaxValue;

            // get page slot in cache
            if (_index.TryGetValue(pageID, out var slot))
            {
                // get all page versions in wal-index
                // and then filter only equals-or-less then selected version
                var v = slot.Keys
                    .Where(x => x <= version)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                // if versions on index are higher then request, exit
                if (v == 0) return long.MaxValue;

                // try get for concurrent dict this page (it's possible this page are no anymore in cache - other concurrency thread clear cache)
                if (slot.TryGetValue(v, out var position))
                {
                    return position;
                }
            }

            return long.MaxValue;
        }

        /// <summary>
        /// Write last confirmation page into all and update all indexes
        /// </summary>
        public void ConfirmTransaction(HeaderPage confirm, IEnumerable<PagePosition> pagePositions)
        {
            // write header-confirm transaction page in wal file
            _datafile.WritePages(new HeaderPage[] { confirm }, false, null);

            // add confirm page into confirmed-queue to be used in checkpoint
            _confirmedTransactions.Add(confirm.TransactionID, confirm);

            // must lock commit operation to update WAL-Index (memory only operation)
            lock (_index)
            {
                // increment current version
                _currentReadVersion++;

                // update wal-index
                foreach (var pos in pagePositions)
                {
                    // get page slot in _index (by pageID) (or create if not exists)
                    var slot = _index.GetOrAdd(pos.PageID, new ConcurrentDictionary<int, long>());

                    // add page version (update if already exists)
                    slot.AddOrUpdate(_currentReadVersion, pos.Position, (v, old) => pos.Position);
                }
            }
        }

        /// <summary>
        /// Do WAL checkpoint coping confirmed pages transaction from WAL file to datafile
        /// </summary>
        public void Checkpoint()
        {
            // # Checkpoint
            // - Ao abrir o banco: vai no LastPageID e percorre todas as paginas a partir do LastPageID e captura as paginas de confirmação
            // ?? Ao iniciar o checkpoint, coloca o banco em modo de reserved completo - não teremos nenhuma transação de escrita durante checkpoint, mas de leitura sim
            // - Posiciona o cursor em LastPageID + 1
            // - Cria uma lista de RunningTransactions Dictionary<Guid, HeaderPage>
            // - Cria lista de contador de locks por coleção Dictionary<string, int>
            // - Para cada pagina lida no WAL (até final do arquivo):
            //     - Se a transactionID não estiver na lista de confirmadas, continue;
            //     - Se estiver na lista, mas não na RunningTransactions:
            //         - Adiciona na lista de execução
            //         - Das os locks de escrita para as coleções incrementando o contador de lock
            //     - Se o transactionID da pagina não estiver na lista, continue;
            //     - Limpa a transactionID
            //     - Grava a pagina na posição correta
            // - Faz shrink do arquivo (se fizer, precisa corrigir o initalSize)?    
            // > Acho que não precisa bloquear novas transações durante o checkpoint, apenas fazer alguns locks antes e depois e usar novas listas (confirmTransaction/wal-index)
            

            // get header from disk (not current header)
            var header = _datafile.ReadPage(0) as HeaderPage;

            // get first page position in wal and datafile length
            var position = BasePage.GetPagePosition(header.LastPageID + 1);
            var length = _datafile.Length;

            // if my position are afer datafile, there is no wal
            if (position >= length) return;

            var running = new HashSet<Guid>();
            var locks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            while(position < length)
            {
                var page = _datafile.ReadPage(position);

                position += BasePage.PAGE_SIZE;

                // continue only if page are in confirm transaction list
                if (!_confirmedTransactions.TryGetValue(page.TransactionID, out var confirm)) continue;

                // test if this transaction are running
                if (!running.Contains(page.TransactionID))
                {
                    running.Add(page.TransactionID);

                    // apply locks
                }

                // this page is confirmation page (last page on transaction)
                if (page.PageID == 0)
                {
                    running.Remove(page.TransactionID);


                }


                // clear transactionID before write on disk 
                page.TransactionID = Guid.Empty;

                // write page on disk
                _datafile.WritePages(new BasePage[] { page }, true, null);
            }

            // read again header to fix file length
            header = _datafile.ReadPage(0) as HeaderPage;

            // shrink datafile and position writer cursor in end of file
            _datafile.Length = BasePage.GetPagePosition(header.LastPageID + 1);

            _datafile.WriterPosition = _datafile.Length;
        }
    }
}