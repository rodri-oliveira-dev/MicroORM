using System;
using System.Collections.Generic;
using DataDrain.Caching.Events;

namespace DataDrain.Caching.Interfaces
{
    public interface ICachingProvider : IDisposable
    {
        void Adicionar<T>(string chave, T valor);
        void Remover(string chave);
        bool Existe(string chave);
        KeyValuePair<bool, T> Recuperar<T>(string chave, bool removerAposRecuperar = false);
        void Clear();
        event CacheChangedEventHandler CacheChanged;
    }
}