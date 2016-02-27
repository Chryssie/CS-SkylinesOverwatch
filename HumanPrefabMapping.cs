using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class HumanPrefabMapping
    {
        private PrefabMapping<uint> _mapping;

        public HumanPrefabMapping()
        {
            _mapping = new PrefabMapping<uint>();
        }

        public List<HashSet<uint>> GetMapping(CitizenInfo human)
        {
            int prefabID = human.m_prefabDataIndex;

            if (!_mapping.PrefabMapped(prefabID))
                CategorizePrefab(human);

            return _mapping.GetMapping(prefabID);
        }

        private void CategorizePrefab(CitizenInfo human)
        {
            CitizenAI ai = human.m_citizenAI;
            int prefabID = human.m_prefabDataIndex;

            /*
             * Create a blank entry. This way, even if this prefab does not belong here
             * for some bizarre reason, we will have a record of it. This eliminates
             * the chance of a prefab getting evaluated more than once, ever.
             */
            _mapping.AddEntry(prefabID);

            if (ai is HumanAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._Humans);

                if (ai is ResidentAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Residents);
                else if (ai is ServicePersonAI)
                    _mapping.AddMapping(prefabID, Data.Instance._ServicePersons);
                else if (ai is TouristAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Tourists);
                else
                    _mapping.AddMapping(prefabID, Data.Instance._HumanOther);
            }
        }
    }
}