using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class BuildingPrefabMapping
    {
        private PrefabMapping<ushort> _mapping;

        public BuildingPrefabMapping()
        {
            _mapping = new PrefabMapping<ushort>();
        }

        public List<HashSet<ushort>> GetMapping(BuildingInfo building)
        {
            int prefabID = building.m_prefabDataIndex;

            if (!_mapping.PrefabMapped(prefabID))
                CategorizePrefab(building);

            return _mapping.GetMapping(prefabID);
        }

        private void CategorizePrefab(BuildingInfo building)
        {
            BuildingAI ai = building.m_buildingAI;
            int prefabID = building.m_prefabDataIndex;

            _mapping.AddMapping(prefabID, Data.Instance._Buildings);

            if (ai is PlayerBuildingAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._PlayerBuildings);

                if (ai is CemeteryAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Cemeteries);
                else if (ai is LandfillSiteAI)
                    _mapping.AddMapping(prefabID, Data.Instance._LandfillSites);
                else if (ai is FireStationAI)
                    _mapping.AddMapping(prefabID, Data.Instance._FireStations);
                else if (ai is PoliceStationAI)
                    _mapping.AddMapping(prefabID, Data.Instance._PoliceStations);
                else if (ai is HospitalAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Hospitals);
                else if (ai is ParkAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Parks);
                else if (ai is PowerPlantAI)
                    _mapping.AddMapping(prefabID, Data.Instance._PowerPlants);
                else
                    _mapping.AddMapping(prefabID, Data.Instance._PlayerOther);
            }
            else if (ai is PrivateBuildingAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._PrivateBuildings);

                if (ai is ResidentialBuildingAI)
                    _mapping.AddMapping(prefabID, Data.Instance._ResidentialBuildings);
                else if (ai is CommercialBuildingAI)
                    _mapping.AddMapping(prefabID, Data.Instance._CommercialBuildings);
                else if (ai is IndustrialBuildingAI)
                    _mapping.AddMapping(prefabID, Data.Instance._IndustrialBuildings);
                else if (ai is OfficeBuildingAI)
                    _mapping.AddMapping(prefabID, Data.Instance._OfficeBuildings);
                else
                    _mapping.AddMapping(prefabID, Data.Instance._PrivateOther);
            }
            else
                _mapping.AddMapping(prefabID, Data.Instance._BuildingOther);
        }
    }
}