using System;
using GridAjaxCRUDMVC.Models;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using DataTables.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Data.Entity;
using System.Threading.Tasks;
using Rotativa;

namespace GridAjaxCRUDMVC.Controllers
{
    public class AssetController : Controller
    {

        private AssetManagementDBEntities _dbContext;

        public AssetManagementDBEntities DbContext
        {
            get
            {
                return _dbContext ?? HttpContext.GetOwinContext().Get<AssetManagementDBEntities>();
            }
            private set
            {
                _dbContext = value;
            }

        }

        public AssetController()
        {

        }

        public AssetController(AssetManagementDBEntities dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Asset
        public ActionResult Index()
        { 

                return View();
        }

                
        
        public ActionResult Get([ModelBinder(typeof(DataTablesBinder))] IDataTablesRequest requestModel, AdvancedSearchViewModel searchViewModel)
        {
            IQueryable<Asset> query = DbContext.Assets;
            var totalCount = query.Count();

            // searching and sorting
            query = SearchAssets(requestModel, searchViewModel, query);
            var filteredCount = query.Count();

            // Paging
            query = query.Skip(requestModel.Start).Take(requestModel.Length);



            var data = query.Select(asset => new
            {
                AssetID = asset.AssetID,
                BarCode = asset.Barcode,
                Manufacturer = asset.Manufacturer,
                ModelNumber = asset.ModelNumber,
                Building = asset.Building,
                RoomNo = asset.RoomNo,
                Quantity = asset.Quantity
            }).ToList();

            return Json(new DataTablesResponse(requestModel.Draw, data, filteredCount, totalCount), JsonRequestBehavior.AllowGet);

        }

       

       // GET: Asset/Create
        public ActionResult Create()
        {
            //Pre populating the dropdown list
            var model = new AssetViewModel();
            model.FacilitySitesSelectList = GetFacilitiySitesSelectList();
            return View("_CreatePartial", model);
        }

        // POST: Asset/Create
        [HttpPost]
        public async Task<ActionResult> Create(AssetViewModel assetVM)
        {
            //Populates the dropdown list with the data from the database
            assetVM.FacilitySitesSelectList = GetFacilitiySitesSelectList();

            //Validation to check if any Model errors are added to ModelState
            if (!ModelState.IsValid)
            {
                return View("_CreatePartial", assetVM);
            }

            Asset asset = MaptoModel(assetVM);

            //Add asset object into Assets DBset
            DbContext.Assets.Add(asset);

            // call SaveChangesAsync method to asynchronously save asset into database
            var task = DbContext.SaveChangesAsync();
            
            await task;
            //Exception Handling - Add error to the ModelState
            if (task.Exception != null)
            {
                ModelState.AddModelError("", "Unable to add the Asset");
                return View("_CreatePartial", assetVM);
            }

            return Content("success");
        }

        // GET: Asset/Edit/5
        public ActionResult Edit(Guid id)
        {
            //Retrieves the record to be edited/Modified
            var asset = DbContext.Assets.FirstOrDefault(x => x.AssetID == id);

            AssetViewModel assetViewModel = MapToViewModel(asset);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_EditPartial", assetViewModel);
            }
            return View(assetViewModel);
        }

        // POST: Asset/Edit/5
        [HttpPost]
        public async Task<ActionResult> Edit(AssetViewModel assetVM)
        {
            //Populates the dropdown list with the data from the database
            assetVM.FacilitySitesSelectList = GetFacilitiySitesSelectList(assetVM.FacilitySiteID);
            //Validation to check if any Model errors are added to ModelState
            if (!ModelState.IsValid)
            {
                //Error Message 
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                // Check if the request is an AJAX call and channel to the view
                return View(Request.IsAjaxRequest() ? "_EditPartial" : "Edit", assetVM);
            }

            //Create a asset object by mapping the AssetViewModel to the underlying database
            Asset asset = MaptoModel(assetVM);

            
            DbContext.Assets.Attach(asset);//State = Unchanged
            //Generates an update stmt to the database to update all columns no matter if the values were modified
            DbContext.Entry(asset).State = EntityState.Modified;
            //Save the changes asynchronously
            var task = DbContext.SaveChangesAsync();
            await task;

            
            if (task.Exception != null)
            {
                ModelState.AddModelError("", "Unable to update the Asset");
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return View(Request.IsAjaxRequest() ? "_EditPartial" : "Edit", assetVM);
            }

            if (Request.IsAjaxRequest())
            {
                return Content("success");
            }

            return RedirectToAction("Index");

        }

        public async Task<ActionResult> Details(Guid id)
        {
            //Retrieves the record for which details are to be displayed
            var asset = await DbContext.Assets.FirstOrDefaultAsync(x => x.AssetID == id);
            var assetVM = MapToViewModel(asset);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_detailsPartial", assetVM);
            }

            return View(assetVM);
        }

        // GET: Asset/Delete/5
        public ActionResult Delete(Guid id)
        {
            //Retrieves the record to be deleted
            var asset = DbContext.Assets.FirstOrDefault(x => x.AssetID == id);

            AssetViewModel assetViewModel = MapToViewModel(asset);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_DeletePartial", assetViewModel);
            }
            return View(assetViewModel);
        }

        // POST: Asset/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteAsset(Guid AssetID)
        {
            var asset = new Asset { AssetID = AssetID };
            DbContext.Assets.Attach(asset);
            DbContext.Assets.Remove(asset);

            var task = DbContext.SaveChangesAsync();
            await task;

            if (task.Exception != null)
            {
                ModelState.AddModelError("", "Unable to Delete the Asset");
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                AssetViewModel assetVM = MapToViewModel(asset);
                return View(Request.IsAjaxRequest() ? "_DeletePartial" : "Delete", assetVM);
            }

            if (Request.IsAjaxRequest())
            {
                return Content("success");
            }

            return RedirectToAction("Index");

        }



        private IQueryable<Asset> SearchAssets(IDataTablesRequest requestModel, AdvancedSearchViewModel searchViewModel, IQueryable<Asset> query)
        {

            // Apply filters
            if (requestModel.Search.Value != string.Empty)
            {
                var value = requestModel.Search.Value.Trim();
                query = query.Where(p => p.Barcode.Contains(value) ||
                                         p.Manufacturer.Contains(value) ||
                                         p.ModelNumber.Contains(value) ||
                                         p.Building.Contains(value)
                                   );
            }

            /***** Advanced Search ******/
            if (searchViewModel.FacilitySite != Guid.Empty)
                query = query.Where(x => x.FacilitySiteID == searchViewModel.FacilitySite);

            if (searchViewModel.Building != null)
                query = query.Where(x => x.Building == searchViewModel.Building);

            if (searchViewModel.Manufacturer != null)
                query = query.Where(x => x.Manufacturer == searchViewModel.Manufacturer);

            if (searchViewModel.Status != null)
            {
                bool Issued = bool.Parse(searchViewModel.Status);
                query = query.Where(x => x.Issued == Issued);
            }

            /***** Advanced Search ******/

            var filteredCount = query.Count();

            // Sort
            var sortedColumns = requestModel.Columns.GetSortedColumns();
            var orderByString = String.Empty;

            foreach (var column in sortedColumns)
            {
                orderByString += orderByString != String.Empty ? "," : "";
                orderByString += (column.Data) + (column.SortDirection == Column.OrderDirection.Ascendant ? " asc" : " desc");
            }

            query = query.OrderBy(orderByString == string.Empty ? "BarCode asc" : orderByString);

            return query;

        }


        [HttpGet]
        public ActionResult AdvancedSearch()
        {
            var advancedSearchViewModel = new AdvancedSearchViewModel();

            advancedSearchViewModel.FacilitySiteList = new SelectList(DbContext.FacilitySites
                                                                    .Where(facilitySite => facilitySite.IsActive && !facilitySite.IsDeleted)
                                                                    .Select(x => new { x.FacilitySiteID, x.FacilityName }),
                                                                      "FacilitySiteID",
                                                                      "FacilityName");

            advancedSearchViewModel.BuildingList = new SelectList(DbContext.Assets
                                                                           .GroupBy(x => x.Building)
                                                                           .Where(x => x.Key != null && !x.Key.Equals(string.Empty))
                                                                           .Select(x => new { Building = x.Key }),
                                                                  "Building",
                                                                  "Building");

            advancedSearchViewModel.ManufacturerList = new SelectList(DbContext.Assets
                                                                               .GroupBy(x => x.Manufacturer)
                                                                               .Where(x => x.Key != null && !x.Key.Equals(string.Empty))
                                                                               .Select(x => new { Manufacturer = x.Key }),
                                                                      "Manufacturer",
                                                                      "Manufacturer");

            advancedSearchViewModel.StatusList = new SelectList(new List<SelectListItem>
            {
                                                                  new SelectListItem { Text="Issued",Value=bool.TrueString},
                                                                  new SelectListItem { Text="Not Issued",Value = bool.FalseString}
                                                                  },
                                                                  "Value",
                                                                  "Text"
                                                                );

            return View("_AdvancedSearchPartial", advancedSearchViewModel);
        }

        //Populates the dropdownlist with the data from the database
        private SelectList GetFacilitiySitesSelectList(object selectedValue = null)
        {
            return new SelectList(DbContext.FacilitySites
                                            .Where(facilitySite => facilitySite.IsActive && !facilitySite.IsDeleted)
                                            .Select(x => new { x.FacilitySiteID, x.FacilityName }),
                                                "FacilitySiteID",
                                                "FacilityName", selectedValue);
        }

        private AssetViewModel MapToViewModel(Asset asset)
        {
            var facilitySite = DbContext.FacilitySites.Where(x => x.FacilitySiteID == asset.FacilitySiteID).FirstOrDefault();

            AssetViewModel assetViewModel = new AssetViewModel()
            {
                AssetID = asset.AssetID,
                Barcode = asset.Barcode,
                AstID = asset.AstID,
                Building = asset.Building,
                ChildAsset = asset.ChildAsset,
                Comments = asset.Comments,
                Corridor = asset.Corridor,
                EquipSystem = asset.EquipSystem,
                FacilitySiteID = asset.FacilitySiteID,
                FacilitySite = facilitySite != null ? facilitySite.FacilityName : String.Empty,
                Floor = asset.Floor,
                GeneralAssetDescription = asset.GeneralAssetDescription,
                Issued = asset.Issued,
                Manufacturer = asset.Manufacturer,
                MERNo = asset.MERNo,
                ModelNumber = asset.ModelNumber,
                PMGuide = asset.PMGuide,
                Quantity = asset.Quantity,
                RoomNo = asset.RoomNo,
                SecondaryAssetDescription = asset.SecondaryAssetDescription,
                SerialNumber = asset.SerialNumber,
                FacilitySitesSelectList = new SelectList(DbContext.FacilitySites
                                                                    .Where(fs => fs.IsActive && !fs.IsDeleted)
                                                                    .Select(x => new { x.FacilitySiteID, x.FacilityName }),
                                                                      "FacilitySiteID",
                                                                      "FacilityName", asset.FacilitySiteID)
            };

            return assetViewModel;
        }

        //Mapping the database table names to the AssetViewModel 
        private Asset MaptoModel(AssetViewModel assetVM)
        {
            Asset asset = new Asset()
            {
                AssetID = assetVM.AssetID,
                Barcode = assetVM.Barcode,
                AstID = assetVM.AstID,
                Building = assetVM.Building,
                ChildAsset = assetVM.ChildAsset,
                Comments = assetVM.Comments,
                Corridor = assetVM.Corridor,
                EquipSystem = assetVM.EquipSystem,
                FacilitySiteID = assetVM.FacilitySiteID,
                Floor = assetVM.Floor,
                GeneralAssetDescription = assetVM.GeneralAssetDescription,
                Issued = assetVM.Issued,
                Manufacturer = assetVM.Manufacturer,
                MERNo = assetVM.MERNo,
                ModelNumber = assetVM.ModelNumber,
                PMGuide = assetVM.PMGuide,
                Quantity = assetVM.Quantity,
                RoomNo = assetVM.RoomNo,
                SecondaryAssetDescription = assetVM.SecondaryAssetDescription,
                SerialNumber = assetVM.SerialNumber
            };

            return asset;
        }

        //Print to PDF
        public ActionResult PrintAllReport()
        {
            var report = new ActionAsPdf("Index");
            return report;
        }

        

    }
}