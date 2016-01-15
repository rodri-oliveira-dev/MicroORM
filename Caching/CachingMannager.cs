using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using DataDrain.Caching.Enuns;
using DataDrain.Caching.Events;
using DataDrain.Caching.Interfaces;

namespace DataDrain.Caching
{
    public sealed class CachingMannager : ICachingProvider
    {
        #region Variaveis e Propriedades

        private readonly MemoryCache _cache;
        private readonly CacheItemPolicy _policy;
        private static readonly object Padlock = new object();

        #endregion

        /// <summary>
        /// Seviço de Cache em memoria 
        /// </summary>
        /// <param name="tempoCache">Tempo que os itens permanecerão em cache a partir do ultimo acesso </param>
        public CachingMannager(TimeSpan tempoCache)
        {
            _cache = new MemoryCache("CachingProvider", new NameValueCollection
            {
                {"pollingInterval", "00:10:00"}, 
                {"physicalMemoryLimitPercentage", "0"},
                {"cacheMemoryLimitMegabytes", "10"}
            });

            _policy = new CacheItemPolicy { SlidingExpiration = tempoCache };
            _policy.RemovedCallback += RemovedCallback;
        }


        #region Métodos base de caching

        /// <summary>
        /// Salva um item em cache
        /// </summary>
        /// <typeparam name="T">Tipo de objeto</typeparam>
        /// <param name="chave">Chave de identificação</param>
        /// <param name="valor">Objeto a ser salvo</param>
        public void Adicionar<T>(string chave, T valor)
        {
            lock (Padlock)
            {
                _cache.Add(chave, valor, _policy);
                OnPropertyChanged(chave, ECacheAcao.Adicionado);
            }
        }

        /// <summary>
        /// Remove um item da cache
        /// </summary>
        /// <param name="chave">Chave de identificação</param>
        public void Remover(string chave)
        {
            lock (Padlock)
            {
                _cache.Remove(chave);
                OnPropertyChanged(chave, ECacheAcao.Removido);
            }
        }

        /// <summary>
        /// Verifica se um item esta salvo na cache
        /// </summary>
        /// <param name="chave">Chave de identificação</param>
        /// <returns>Boleano de confirmação</returns>
        public bool Existe(string chave)
        {
            lock (Padlock)
            {
                var res = _cache[chave];

                return res != null;
            }
        }

        /// <summary>
        /// Recupera um item da Cache
        /// </summary>
        /// <typeparam name="T">Tipo do objeto recuperado</typeparam>
        /// <param name="chave">Chave de identificação</param>
        /// <param name="removerAposRecuperar">Indica se deve remover da cache ao recuperar, por padrão é false</param>
        /// <returns>Conjunto chave valor [boleano,Objeto] onde boleano indica se foi possivel recuperar</returns>
        public KeyValuePair<bool, T> Recuperar<T>(string chave, bool removerAposRecuperar = false)
        {
            var retorno = Recuperar(chave, removerAposRecuperar);

            return new KeyValuePair<bool, T>(retorno.Key, retorno.Value != null ? (T)retorno.Value : default(T));
        }

        /// <summary>
        /// Recupera um item da Cache
        /// </summary>
        /// <param name="chave">Chave de identificação</param>
        /// <param name="removerAposRecuperar">Indica se deve remover da cache ao recuperar, por padrão é false</param>
        /// <returns>Conjunto chave valor [boleano,Objeto] onde boleano indica se foi possivel recuperar</returns>
        public KeyValuePair<bool, object> Recuperar(string chave, bool removerAposRecuperar = false)
        {
            lock (Padlock)
            {
                var res = _cache[chave];

                if (res != null)
                {
                    if (removerAposRecuperar)
                    {
                        _cache.Remove(chave);
                    }
                }

                return new KeyValuePair<bool, object>(res != null, res);
            }
        }

        /// <summary>
        /// Limpa todas as chaves da cache
        /// </summary>
        public void Clear()
        {
            lock (Padlock)
            {
                foreach (var cacheKey in _cache.Select(kvp => kvp.Key))
                {
                    _cache.Remove(cacheKey);
                }
            }
        }

        #endregion

        #region Evento de status

        /// <summary>
        /// Informa eventos com os itens da cache
        /// </summary>
        public event CacheChangedEventHandler CacheChanged;

        private void OnPropertyChanged(string propertyName, ECacheAcao acao)
        {
            var handler = CacheChanged;
            if (handler != null) handler(this, new CacheChangedEventArgs(propertyName, acao));
        }

        private void RemovedCallback(CacheEntryRemovedArguments arguments)
        {
            OnPropertyChanged(arguments.CacheItem.Key, ECacheAcao.Expirou);
        }

        #endregion

        /// <summary>
        /// Libera o serviço de cache
        /// </summary>
        public void Dispose()
        {
            lock (Padlock)
            {
                _cache.Dispose();
            }
        }
    }
}
