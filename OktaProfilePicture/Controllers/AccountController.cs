using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Okta.AspNetCore;
using Okta.Sdk;
using OktaProfilePicture.Models;

namespace OktaProfilePicture.Controllers
{
    public class AccountController : Controller
    {
        private readonly OktaClient _oktaClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly FaceClient _faceClient;

        public AccountController(OktaClient oktaClient, BlobServiceClient blobServiceClient, FaceClient faceClient)
        {
            _oktaClient = oktaClient;
            _faceClient = faceClient;

            _blobContainerClient = blobServiceClient.GetBlobContainerClient("okta-profile-picture-container2");
            _blobContainerClient.CreateIfNotExists();
        }
        
        public IActionResult LogIn()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Challenge(OktaDefaults.MvcAuthenticationScheme);
            }

            return RedirectToAction("Index", "Home");
        }
        
        public IActionResult LogOut()
        {
            return new SignOutResult(
                new[]
                {
                    OktaDefaults.MvcAuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                },
                new AuthenticationProperties { RedirectUri = "/Home/" });
        }
        
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await GetOktaUser();
            var profileImage = (string)user.Profile["profileImageKey"];
            if (string.IsNullOrEmpty(profileImage))
            {
                return View(user);
            }

            var sasBuilder = new BlobSasBuilder
            {
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var url = _blobContainerClient.GetBlobClient(profileImage).GenerateSasUri(sasBuilder);
            ViewData["ProfileImageUrl"] = url;

            return View(user);
        }
        
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await GetOktaUser();

            return View(new UserProfileViewModel()
            {
                City = user.Profile.City,
                Email = user.Profile.Email,
                CountryCode = user.Profile.CountryCode,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName
            });
        }
        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UserProfileViewModel profile)
        {
            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            var user = await GetOktaUser();
            user.Profile.FirstName = profile.FirstName;
            user.Profile.LastName = profile.LastName;
            user.Profile.Email = profile.Email;
            user.Profile.City = profile.City;
            user.Profile.CountryCode = profile.CountryCode;

            if (profile.ProfileImage == null)
            {
                await _oktaClient.Users.UpdateUserAsync(user, user.Id, null);
                return RedirectToAction("Profile");
            }

            var stream = profile.ProfileImage.OpenReadStream();
            var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: RecognitionModel.Recognition04, detectionModel: DetectionModel.Detection01);

            if (detectedFaces.Count != 1 || detectedFaces[0].FaceId == null)
            {
                ModelState.AddModelError("", $"Detected {detectedFaces.Count} faces instead of 1 face");
                return View(profile);
            }

            var personGroupId = user.Id.ToLower();

            if (string.IsNullOrEmpty((string)user.Profile["personId"]))
            {
                await _faceClient.PersonGroup.CreateAsync(personGroupId, user.Profile.Login, recognitionModel: RecognitionModel.Recognition04);

                stream = profile.ProfileImage.OpenReadStream();
                var personId = (await _faceClient.PersonGroupPerson.CreateAsync(personGroupId, user.Profile.Login)).PersonId;
                await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, personId, stream);

                user.Profile["personId"] = personId;
                await UpdateUserImage();
            }
            else
            {
                var faceId = detectedFaces[0].FaceId.Value;

                var personId = new Guid((string)user.Profile["personId"]);
                var verifyResult = await _faceClient.Face.VerifyFaceToPersonAsync(faceId, personId, personGroupId);

                if (verifyResult.IsIdentical && verifyResult.Confidence >= 0.8)
                {
                    await UpdateUserImage();
                }
                else
                {
                    ModelState.AddModelError("", "The uploaded picture doesn't match your current picture");
                    return View(profile);
                }
            }
            
            await _oktaClient.Users.UpdateUserAsync(user, user.Id, null);
            return RedirectToAction("Profile");

            async Task UpdateUserImage()
            {
                var blobName = Guid.NewGuid().ToString();
                if (!string.IsNullOrEmpty((string)user.Profile["profileImageKey"]))
                {
                    await _blobContainerClient.DeleteBlobAsync((string)user.Profile["profileImageKey"]);
                }
                await _blobContainerClient.UploadBlobAsync(blobName, profile.ProfileImage?.OpenReadStream());
                user.Profile["profileImageKey"] = blobName;
            }
        }
        
        [Authorize]
        public async Task<IActionResult> DeleteProfilePic()
        {
            var user = await GetOktaUser();

            await CleanAzureResources();

            user.Profile["profileImageKey"] = null;
            user.Profile["personId"] = null;
            await _oktaClient.Users.UpdateUserAsync(user, user.Id, null);
            return RedirectToAction("Profile");

            async Task CleanAzureResources()
            {
                // remove image from blob
                await _blobContainerClient.DeleteBlobAsync((string)user.Profile["profileImageKey"]);

                // remove face from Face services
                var personId = Guid.Parse((string)user.Profile["personId"]);
                await _faceClient.PersonGroupPerson.DeleteAsync(user.Id.ToLower(), personId);
                await _faceClient.PersonGroup.DeleteAsync(user.Id.ToLower());
            }
        }
        
        private async Task<IUser> GetOktaUser()
        {
            var subject = HttpContext.User.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value;
            return await _oktaClient.Users.GetUserAsync(subject);
        }
    }
}