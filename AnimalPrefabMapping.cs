using System;
using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class AnimalPrefabMapping
    {
        private Dictionary<string, int> _prefabs;

        private PrefabMapping<ushort> _mapping;

        public AnimalPrefabMapping()
        {
            _prefabs = new Dictionary<string, int>();
            LoadTrackedPrefabs();

            _mapping = new PrefabMapping<ushort>();
        }

        public List<HashSet<ushort>> GetMapping(CitizenInfo animal)
        {
            int prefabID = animal.m_prefabDataIndex;

            if (!_mapping.PrefabMapped(prefabID))
                CategorizePrefab(animal);

            return _mapping.GetMapping(prefabID);
        }

        private void LoadTrackedPrefabs()
        {
            for (uint i = 0; i < PrefabCollection<CitizenInfo>.PrefabCount(); i++)
            {
                CitizenInfo prefab = PrefabCollection<CitizenInfo>.GetPrefab(i);

                if (prefab == null)
                    continue;

                string name = prefab.GetLocalizedTitle();

                if (String.IsNullOrEmpty(name))
                    continue;

                if (!Settings.Instance.Animals.Contains(name))
                    continue;

                if (_prefabs.ContainsKey(name))
                    continue;

                _prefabs.Add(name, (int)i);
            }
        }

        private void CategorizePrefab(CitizenInfo animal)
        {
            CitizenAI ai = animal.m_citizenAI;
            int prefabID = animal.m_prefabDataIndex;

            /*
             * Create a blank entry. This way, even if this prefab does not belong here
             * for some bizarre reason, we will have a record of it. This eliminates
             * the chance of a prefab getting evaluated more than once, ever.
             */
            _mapping.AddEntry(prefabID);

            if (ai is AnimalAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._Animals);

                if (ai is BirdAI)
                {
                    _mapping.AddMapping(prefabID, Data.Instance._Birds);

                    if (_prefabs.ContainsKey("Seagull") && _prefabs["Seagull"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Seagulls);
                }
                else if (ai is LivestockAI)
                {
                    _mapping.AddMapping(prefabID, Data.Instance._Livestock);

                    if (_prefabs.ContainsKey("Cow") && _prefabs["Cow"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Cows);

                    if (_prefabs.ContainsKey("Pig") && _prefabs["Pig"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Pigs);
                }
                else if (ai is PetAI)
                {
                    _mapping.AddMapping(prefabID, Data.Instance._Pets);

                    if (_prefabs.ContainsKey("Dog") && _prefabs["Dog"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Dogs);
                }
                else if (ai is WildlifeAI)
                {
                    _mapping.AddMapping(prefabID, Data.Instance._Wildlife);

                    if (_prefabs.ContainsKey("Wolf") && _prefabs["Wolf"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Wolves);

                    if (_prefabs.ContainsKey("Bear") && _prefabs["Bear"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Bears);

                    if (_prefabs.ContainsKey("Moose") && _prefabs["Moose"] == prefabID)
                        _mapping.AddMapping(prefabID, Data.Instance._Moose);
                }
                else
                    _mapping.AddMapping(prefabID, Data.Instance._AnimalOther);
            }
        }
    }
}