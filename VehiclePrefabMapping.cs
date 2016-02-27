using System.Collections.Generic;

namespace SkylinesOverwatch
{
    public class VehiclePrefabMapping
    {
        private PrefabMapping<ushort> _mapping;

        public VehiclePrefabMapping()
        {
            _mapping = new PrefabMapping<ushort>();
        }

        public List<HashSet<ushort>> GetMapping(VehicleInfo vehicle)
        {
            int prefabID = vehicle.m_prefabDataIndex;

            if (!_mapping.PrefabMapped(prefabID))
                CategorizePrefab(vehicle);

            return _mapping.GetMapping(prefabID);
        }

        private void CategorizePrefab(VehicleInfo vehicle)
        {
            VehicleAI ai = vehicle.m_vehicleAI;
            int prefabID = vehicle.m_prefabDataIndex;

            _mapping.AddMapping(prefabID, Data.Instance._Vehicles);

            if (ai is CarTrailerAI)
                return;
            else if (ai is CarAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._Cars);

                if (ai is HearseAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Hearses);
                else if (ai is GarbageTruckAI)
                    _mapping.AddMapping(prefabID, Data.Instance._GarbageTrucks);
                else if (ai is FireTruckAI)
                    _mapping.AddMapping(prefabID, Data.Instance._FireTrucks);
                else if (ai is PoliceCarAI)
                    _mapping.AddMapping(prefabID, Data.Instance._PoliceCars);
                else if (ai is AmbulanceAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Ambulances);
                else if (ai is BusAI)
                    _mapping.AddMapping(prefabID, Data.Instance._Buses);
                else
                    _mapping.AddMapping(prefabID, Data.Instance._CarOther);
            }
            else if (ai is TrainAI)
            {
                _mapping.AddMapping(prefabID, Data.Instance._Trains);

                if (ai is MetroTrainAI)
                    _mapping.AddMapping(prefabID, Data.Instance._MetroTrains);
                else if (ai is PassengerTrainAI)
                    _mapping.AddMapping(prefabID, Data.Instance._PassengerTrains);
                else if (ai is CargoTrainAI)
                    _mapping.AddMapping(prefabID, Data.Instance._CargoTrains);
                else
                    _mapping.AddMapping(prefabID, Data.Instance._TrainOther);
            }
            else if (ai is AircraftAI)
                _mapping.AddMapping(prefabID, Data.Instance._Aircraft);
            else if (ai is ShipAI)
                _mapping.AddMapping(prefabID, Data.Instance._Ships);
            else
                _mapping.AddMapping(prefabID, Data.Instance._VehicleOther);
        }
    }
}