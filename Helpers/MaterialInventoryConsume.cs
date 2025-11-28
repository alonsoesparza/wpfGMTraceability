using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Helpers
{
    public class MaterialInventoryConsume
    {
        StationData BOMData;
        public MaterialInventoryConsume()
        {
        }
        public async Task<StationData> LoadBOMDataAsync()
        {
            try
            {
                BOMData = await ApiCalls.GetStationDataAsync();
                return BOMData;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public List<object> CheckForSufficientStock()
        {
            try
            {
                var SufficientParts = BOMData.Parts
                        .Where(p => !p.Sufficient)
                        .Select(p => new
                        {
                            p.BomPart,
                            p.bom_quantity_per_piece,
                            p.total_available
                        });

                return SufficientParts.Cast<object>().ToList();
            }
            catch (Exception Ex)
            {
                return null;
            }
        }
        public async Task<(string serial, string message, string statusCode, string typeMsj)> DoConsume(string serial)
        {
            string msjResult;
            //** Cajas Ordenadas para recorrerlas en orden (Ojo, tambien tiene el BOM)
            List<Part> orderedBoxesByPart = BOMData.Parts
                                                    .Where(p => p.Boxes != null && p.Boxes.Count > 0)
                                                    .Select(p => new Part
                                                    {
                                                        BomPart = p.BomPart,
                                                        bom_quantity_per_piece = p.bom_quantity_per_piece,
                                                        total_available = p.total_available,
                                                        Sufficient = p.Sufficient,
                                                        Boxes = p.Boxes.OrderBy(b => b.BoxNumber).ToList()
                                                    })
                                                    .ToList();
            //*****Lista de Material Faltante
            List<Box> missingItems = new List<Box>();

            //** Crear la lista de consumo
            var consumptionItems = new List<object>();
            foreach (Part part in orderedBoxesByPart)
            {
                int qtyToConsume = (int)part.bom_quantity_per_piece;
                foreach (Box box in part.Boxes)
                {
                    int remainingQty = (int)box.BoxQt;
                    if (qtyToConsume <= remainingQty)
                    {
                        consumptionItems.Add(new
                        {
                            boxnumber = box.BoxNumber,
                            serialtestnumber = serial,
                            qty = qtyToConsume
                        });

                        //Borrar el Part de la lista si en la anterior interaccion no tenia inventario disponible (cuando hay varias cajas y ya se acabo alguna)
                        var boxToRemove = missingItems.FirstOrDefault(b => b.Part == part.BomPart);
                        if (boxToRemove != null) { missingItems.Remove(boxToRemove); }
                        break;
                    }else if (qtyToConsume > remainingQty || remainingQty > 0) {

                        consumptionItems.Add(new
                        {
                            boxnumber = box.BoxNumber,
                            serialtestnumber = serial,
                            qty = remainingQty
                        });
                        qtyToConsume = qtyToConsume - remainingQty;
                        remainingQty = 0;
                    }
                    else
                    {
                        missingItems.Add(new Box
                        {
                            Part = part.BomPart,
                            BoxNumber = "NA",
                            BoxQt = 0
                        });
                    }
                }
            }

            if(missingItems.Count > 0)
            {
                //****NO PUEDO SEGUIR CON EL CONSUMO POR QUE HAY UN np QUE NO TIENE INVENTARIO
                return (null, null, null, null);
            }
            else
            {
                var finalJson = new
                {
                    station_name = BOMData.Station,
                    items = consumptionItems
                };

                string jsonFinal = JsonConvert.SerializeObject(finalJson, Formatting.Indented);
                var result = await ApiCalls.PostAPIConsumeAsync(jsonFinal);

                string ResContent = result.content;
                int StatusCode = result.statusCode;
                //string StatusMessage = HttpStatusHelper.GetStatusMessage(StatusCode);

                string sType;
                if(ResContent != null) { sType = "OK"; } else { sType = "ERROR"; }
                //Task<(string serial, string message, string statusCode, string typeMsj)>
                return (serial, $"CONSUMPTION {sType}", StatusCode.ToString().Trim(), sType);
            }
        }
    }
}
