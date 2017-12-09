using Halite2.hlt;
using System.Collections.Generic;
using System.Linq;

namespace Halite2
{
    public class MyBot
    {

        public static void Main(string[] args)
        {
            string name = args.Length > 0 ? args[0] : "Sharpie";

            Networking networking = new Networking();
            GameMap gameMap = networking.Initialize(name);

            List<Move> moveList = new List<Move>();
            for (; ; )
            {
                moveList.Clear();
                try
                {
                    gameMap.UpdateMap(Networking.ReadLineIntoMetadata());
                }
                catch
                {
                    break;
                }

                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    switch (ship.GetDockingStatus())
                    {
                        case Ship.DockingStatus.Docked:
                            if (gameMap.GetAllPlanets().Where(p => p.Key == ship.GetDockedPlanet()).Single().Value.GetDockedShips().Count > 8)
                            {
                                moveList.Add(new UndockMove(ship));
                            }
                            continue;

                        case Ship.DockingStatus.Docking:
                            continue;

                        case Ship.DockingStatus.Undocked:
                            if (!gameMap.GetAllPlanets().Where(p => !p.Value.IsOwned()).Any())  // no unowned planets
                            {
                                // get the closest planet owned by me
                                Planet newPlanet = gameMap.GetAllPlanets().OrderBy(p => p.Value.GetDistanceTo(ship)).FirstOrDefault(p => p.Value.GetOwner() != ship.GetOwner()).Value;
                                if (newPlanet == null)
                                    break;
                                int targetShipId = newPlanet.GetDockedShips().First();
                                int ownerId = newPlanet.GetOwner();

                                // Pick a ship at the target planet
                                Ship newTarget = gameMap.GetAllPlayers().Single(p=>p.GetId() == ownerId).GetShip(targetShipId);
                                ThrustMove newMove = new ThrustMove(ship, ship.OrientTowardsInDeg(newTarget), Constants.MAX_SPEED);
                                moveList.Add(newMove);
                                continue;
                            }

                            foreach (Planet planet in gameMap.GetAllPlanets().Values.OrderBy(p => p.GetDistanceTo(ship)))
                            {
                                if (planet.IsOwned())
                                {
                                    continue;
                                }

                                if (ship.CanDock(planet))
                                {
                                    moveList.Add(new DockMove(ship, planet));
                                    break;
                                }

                                ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED);
                                if (newThrustMove != null)
                                {
                                    moveList.Add(newThrustMove);
                                }

                                break;
                            }
                            continue;

                        case Ship.DockingStatus.Undocking:
                            continue;
                    }                        

                }

                Networking.SendMoves(moveList);
            }
        }
    }
}
