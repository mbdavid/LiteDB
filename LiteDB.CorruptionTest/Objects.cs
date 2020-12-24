using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace CorruptionTest
{
    public class Objects
    {
        public class job
        {
            [LiteDB.BsonId]
            // LiteDB id
            public Int64 litedb_id { get; set; }


            // A unique id made up of the computer's client_id and it's job id
            public string unique_id
            {
                get => string.Format("{0}-{1}-{2}", created.ToString("yy"), device_info.short_id, litedb_id);
            }
            //
            public string description { get; set; }
            //
            public List<job_event> event_log { get; set; }
            // 
            public job_status status { get; set; }
            // The job has been completed and system cleaned up
            [ObsoleteAttribute("No longer in use 11/03/2020")]
            public bool cleaned_up { get; set; }
            // All user info
            public fw_user user_info { get; set; }
            // Device info
            public local_device device_info { get; set; }
            // The id of the template used
            public string template_id { get; set; }
            // The date/time the job was created
            public DateTime created { get; set; }
            public DateTime? completed_datetime { get; set; } = null;
            // The date/time the job was last saved to the local db
            public DateTime last_save { get; set; }
            // The date/time the job was last synced with the server
            public DateTime? last_sync { get; set; }
            // List opf files the job
            public ObservableCollection<file> files { get; set; }
            // Metadata for the job | iptc, value(s)
            public Dictionary<string, List<string>> lookup_metadata { get; set; }
            //
            public album_creation fotoweb_album { get; set; } = null;
            //
            public job_consent consent { get; set; }
            //
            public string error_reason { get; set; } = string.Empty;
            //
            public int retry_count { get; set; } = 0;
            //
            public Dictionary<string, int> error_counters { get; set; } = new Dictionary<string, int>();
            //
            public DateTime? deleted_datetime { get; set; } = null;
            //
            public DateTime? delete_when { get; set; } = null;
            //
            [LiteDB.BsonIgnore]
            public string progress { get; set; }
            //
            [LiteDB.BsonIgnore]
            public string action { get; set; }
        }



        #region Sub Objects
        public class job_event
        {
            public DateTime created { get; set; }
            public string type { get; set; }
            public Dictionary<string, List<string>> data { get; set; }
        }

        public class fw_user
        {
            public string href { get; set; }
            public string userName { get; set; }
            public string fullName { get; set; }
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string userId { get; set; }
            public string isGuest { get; set; }
            public string userAvatarHref { get; set; }
        }

        public class local_device
        {
            [LiteDB.BsonId]
            public LiteDB.ObjectId id { get; set; }
            public int short_id { get; set; }
            public string device_name { get; set; }             // The computer name
            public string token { get; set; }
            public string type { get; set; }
            public string operating_system { get; set; }        // Windows/ version etc / Android/IOS etc
            public string manufacturer { get; set; } = null;    // Samsung etc
            public string model { get; set; } = null;           // Galaxy / Note or model number etc
        }

        public class file
        {
            public string id { get; set; }                                  // ID of the file
            public string filename { get; set; }                            // Full filename of the file including extention
            public string extension { get; set; }
            public string file_checksum { get; set; }                       // This will contain the hash of the file e.g SHA1:BE2C127A49C7F116270F26E6444FAB330E1B5246 or SHA256:70A018813401B9A68E647D9D971481B81202480557F02CF53A1FB579AB75EECC        
            public local_file_info local_file_info { get; set; }            // Information about the local file        
            public long filesize_bytes { get; set; }                        // File size in bytes
            public string friendly_filesize
            {
                get
                {
                    //return "woo :" + filesize_bytes;
                    string result = "";
                    try
                    {
                        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                        double len = double.Parse(filesize_bytes.ToString());
                        int order = 0;
                        while (len >= 1024 && order < sizes.Length - 1)
                        {
                            order++;
                            len = len / 1024;
                        }

                        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                        // show a single decimal place, and no space.
                        result = String.Format("{0:0.#} {1}", len, sizes[order]);
                    }
                    catch// (Exception ex)
                    {
                        result = "0";
                    }

                    return result;
                }
            }                              // Frendly filesize in KB/MB/GB etc                           
            public Dictionary<string, List<string>> metadata { get; set; }  // Metadata for the file | iptc, value(s)
            public file_status status { get; set; } = file_status.CREATED;
            public bool file_valid { get; set; }
            public fileuploadtask workingcopy { get; set; }
            public fileuploadtask mastercopy { get; set; }
            public link_info link { get; set; } = null;
            public DateTime? completed_datetime { get; set; } = null;
            public string master_of { get; set; } = string.Empty;
            public asset_type type { get; set; }
            public string in_group { get; set; } = null;

            [LiteDB.BsonIgnore]
            public List<CustomContextMenu> ContextMenuItems { get; set; }
            public List<fw_backgroundtask_response> fotoweb_responses { get; set; } = new List<fw_backgroundtask_response>();

            //public file() { }
            //public file(bool random)
            //{
            //    if (!random) throw new NotImplementedException();

            //    id = new Guid().ToString();
            //    filename = "NEWFILEAAAAA.JPG";
            //    extension = ".JPG";
            //    file_checksum = "SHA1:0000000000000000000000000000000000000000";

            //}
        }

        public class CustomContextMenu
        {
            public string header { get; set; } = string.Empty;
            public string icon { get; set; } = string.Empty;
            public CustomContextMenuType type { get; set; }
            public string text_colour { get; set; }
        }

        public class local_file_info
        {
            // Datetime the file was imported on the local system (local system time - could be wrong)
            public DateTime imported_datetime { get; set; }
            public DateTime system_created_datetime { get; set; }
            // Drive or local folder the file was located in
            public string original_file_path { get; set; }
            // Where the file is on the local system before zipping and uploading
            public string local_file_path { get; set; }
            public string original_file_directory { get; set; }
            public string thumbnail_path { get; set; }
        }

        public class link_info
        {
            public string child_guid { get; set; }
            [LiteDB.BsonIgnore]
            public bool is_child { get; set; } = false;
            public bool has_child { get; set; } = false;
        }

        public class fileuploadtask
        {
            public bool? Complete { get; set; } = null;
            public int RetryCount { get; set; } = 0;
            public string BackgroundTaskUrl { get; set; } = string.Empty;
            public fw_backgroundtask_response BackgroundTaskResponse { get; set; }
        }

        public class album_creation
        {
            public string album_href { get; set; }
            public int retry_counter { get; set; } = 0;
            public List<string> asset_guid { get; set; }    // List of guids in the correct order that the user has chosen
            public album info { get; set; }
            public album_status status { get; set; } = album_status.PENDING;
        }

        public class album
        {
            public string album_title { get; set; }         // Can contain smart tags
            public string album_description { get; set; }
            public string email_title { get; set; }         // Title of the email FotoWeb will send
            public string email_body { get; set; }          // Body of the email FotoWeb will send
            public List<fw_person_result> People { get; set; } = new List<fw_person_result>();
            public bool should_email { get; set; }
        }

        public class job_consent
        {
            public consent_option selected_option { get; set; }
            public string signee_type { get; set; }
            public string signee_name { get; set; }
            public DateTime signed_time { get; set; }
            public string signature_path { get; set; }
        }

        public class consent_option
        {
            public string guid { get; set; } = "";
            public string name { get; set; }
            public string short_description { get; set; }
            public string description { get; set; }
            public bool require_signature { get; set; } = false;
            public bool require_name { get; set; } = false;
            public List<string> signee_type { get; set; } = new List<string>();
            public bool create_pdf { get; set; } = false;
            public string pdf_template_name { get; set; } = string.Empty;
        }

        public enum job_status
        {
            CREATED = 0,    // CREATED - added but not uploaded
            PENDING = 1,    // PENDING - All
            COMPLETE = 2,   // COMPLETE
            ERROR = 3,      // ERROR - something wrong
            DELETED = 4,    // Deleted
            ERRORFORBIDDEN = 5,
            ERRORAUTHORIZATION = 6,
            ERRORNOTFOUND = 7,
            PERMANANTDELETE = 8
        }

        public enum asset_type
        {
            IMAGE = 0,
            VIDEO = 1,
            AUDIO = 2,
            DOCUMENT = 3,
            OTHER = 4,
            IMAGEANNOTATION = 5,
            FILE = 6,
            MASTERFILE = 8,
            CONSENTPDF = 9
        }

        public enum album_status
        {
            PENDING = 0,
            COMPLETE = 1,
            ERROR = 2,          // General error
            ERRORCREATE = 3,    // Error creating the labum
            ERRORADD = 4,        // Error adding files
            ERRORSHARE = 5
        }

        public enum CustomContextMenuType
        {
            Intergration,
            Delete
        }

        public enum file_status
        {
            CREATED = 0,    // CREATED - added but not uploaded
            COMPLETE = 2,   // COMPLETE
            ERROR = 3      // ERROR - something wrong
        }


        #region FW_Objects

        public class fw_person_result
        {
            public string display_name { get; set; }
            public string href { get; set; }
            public string type { get; set; }
            public string unique_name { get; set; }
        }

        public class fw_backgroundtask_response
        {
            public fw_backgroundtasks_Job job { get; set; }
            public fw_backgroundtasks_Task task { get; set; }
        }

        public class fw_backgroundtasks_Task
        {
            public string status { get; set; }
            public string created { get; set; }
            public string modified { get; set; }
            public string href { get; set; }
            public string user { get; set; }
            public string type { get; set; }
            public string id { get; set; }

        }

        public class fw_backgroundtasks_Job
        {
            public string status { get; set; }

            public List<fw_backgroundtasks_JobResult> result { get; set; }
        }

        public class fw_backgroundtasks_JobResult
        {
            public string errorCode { get; set; }
            public string errorMessage { get; set; }
            public string href { get; set; }
            public bool done { get; set; }
            public fw_asset asset { get; set; }

        }

        public class fw_asset
        {
            public string href { get; set; }
            public string archiveHREF { get; set; }
            public string linkstance { get; set; }
            public string created { get; set; }
            public string filename { get; set; }
            public string filesize { get; set; }
            public List<string> permissions { get; set; }
            public string doctype { get; set; }
            public List<fw_preview> previews { get; set; }
            public fw_metadata_editor metadataEditor { get; set; }

            public fw_asset_attributes attributes { get; set; }

            public Dictionary<string, fw_metadata_value> metadata { get; set; }

            public fw_asset_page_info pages { get; set; }


            public List<fw_preview> quickRenditions { get; set; }

            public bool IsSelected { get; set; }

            public List<fw_rendition> renditions { get; set; }
        }

        public class fw_rendition
        {
            public string display_name { get; set; }
            public string description { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string href { get; set; }
            public bool original { get; set; }
            public bool sizeFixed { get; set; }
            public string profile { get; set; }
        }

        public class fw_preview
        {
            public int size { get; set; }
            public string width { get; set; }
            public string height { get; set; }
            public string href { get; set; }
            public bool square { get; set; }
            public string name { get; set; } = "";
        }

        public class fw_metadata_editor
        {
            public string name { get; set; }
            public string href { get; set; }

            public string id { get; set; }

            public List<fw_metadata_region> detailRegions { get; set; }
            public fw_metadata_thumbnailFields thumbnailFields { get; set; }
        }

        public class fw_metadata_value
        {
            //[JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> value { get; set; }
        }
        //class SingleOrArrayConverter<T> : JsonConverter
        //{
        //    public override bool CanConvert(Type objectType)
        //    {
        //        return (objectType == typeof(List<T>));
        //    }

        //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //    {
        //        JToken token = JToken.Load(reader);
        //        if (token.Type == JTokenType.Array)
        //        {
        //            return token.ToObject<List<T>>();
        //        }
        //        return new List<T> { token.ToObject<T>() };
        //    }

        //    public override bool CanWrite
        //    {
        //        get { return false; }
        //    }

        //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public class fw_asset_page_info
        {
            public string href { get; set; }
            public string pageTemplate { get; set; }
            public string asset { get; set; }
            public fw_paging paging { get; set; }
            public List<fw_asset_page> data { get; set; }
        }

        public class fw_asset_page
        {
            public string href { get; set; }
            public string pageTemplate { get; set; }
            public string page { get; set; }
            public List<fw_preview> previews { get; set; }
            public fw_paging pages { get; set; }
        }

        public class fw_paging
        {
            public string prev { get; set; }
            public string next { get; set; }
            public string first { get; set; }
            public string last { get; set; }
        }

        public class fw_metadata_thumbnailFields
        {
            public fw_metadata_item secondLine { get; set; }
            public fw_metadata_item firstLine { get; set; }
            public fw_metadata_item label { get; set; }
            public List<fw_metadata_item> additionalFields { get; set; }

        }

        public class fw_metadata_region
        {
            public string name { get; set; }
            public List<fw_metadata_item> fields { get; set; }
        }

        public class fw_metadata_item
        {
            public bool taxonomyonly { get; set; }
            public bool isWritable { get; set; }
            public bool required { get; set; }
            public fw_metadata_field field { get; set; }

            public string valueStore { get; set; }

            public List<fw_taxonomy_item> taxonomy_items { get; set; }

            //Used by medialogix only. Not delivered from Fotoweb.
            public List<string> _valueHelper { get; set; } = new List<string>();
        }

        public class fw_metadata_field
        {
            public string label { get; set; }
            public int id { get; set; }
            //[JsonProperty("max-size")]
            public int maxsize { get; set; }
            public bool multiline { get; set; }
            //[JsonProperty("data-type")]
            public string datatype { get; set; }
            public string taxonomyHref { get; set; }
            public fw_field_validation validation { get; set; }

            //[JsonProperty("multi-instance")]
            public bool multiinstance { get; set; }
        }

        public class fw_field_validation
        {
            public string regexp { get; set; }
            public int? max { get; set; }
            public int? min { get; set; }
        }

        public class fw_taxonomy_item
        {
            public int field { get; set; }
            public string value { get; set; }
            public string label { get; set; }
            public string description { get; set; }
            public string href { get; set; }
            public bool hasChildren { get; set; }

            public List<fw_taxonomy_item> children { get; set; }
        }

        public class fw_asset_attributes
        {
            public fw_video_attributes videoattributes { get; set; }

            public fw_image_attributes imageattributes { get; set; }

            public fw_photo_attributes photoAttributes { get; set; }

            public fw_document_attributes documentattributes { get; set; }

            public fw_audio_attributes audioattributes { get; set; }
        }

        public class fw_image_attributes
        {
            public string pixelwidth { get; set; }
            public string pixelheight { get; set; }
            public string resolution { get; set; }
            public string flipmirror { get; set; }
            public string rotation { get; set; }
            public string colorspace { get; set; }

        }

        public class fw_photo_attributes
        {
            public string cameraModel { get; set; }
            public string fNumber { get; set; }
            public string focalLength { get; set; }
            public string isoSpeed { get; set; }

            public fw_flash_attributes flash { get; set; }


        }

        public class fw_flash_attributes
        {
            public bool fired { get; set; }
        }

        public class fw_document_attributes
        {
            public string pages { get; set; }
        }

        public class fw_audio_attributes
        {
            public string status { get; set; }
            public fw_audio_attributes_info attributes { get; set; }
            public fw_audio_attributes_proxy proxy { get; set; }

        }

        public class fw_audio_attributes_info
        {
            public string bitsPerSample { get; set; }
            public string samplingRate { get; set; }
            public string channels { get; set; }
            public string duration { get; set; }
            public string compressorID { get; set; }
            public string compressorName { get; set; }
            public string bitrate { get; set; }


        }

        public class fw_video_attributes
        {
            public string status { get; set; }
            public fw_video_attributes_info attributes { get; set; }
            public fw_video_attributes_proxy proxy { get; set; }
        }

        public class fw_audio_attributes_proxy
        {
            public string href { get; set; }


        }

        public class fw_video_attributes_info
        {
            public string imageWidth { get; set; }
            public string imageHeight { get; set; }
            public string duration { get; set; }
            public string totalFrames { get; set; }
            public string frameRate { get; set; }
            public string compressorID { get; set; }
            public string compressorName { get; set; }
            public string bitrate { get; set; }

        }

        public class fw_video_attributes_proxy
        {
            public string videoHREF { get; set; }
            public string imageWidth { get; set; }
            public string imageHeight { get; set; }
            public string duration { get; set; }
            public string totalFrames { get; set; }
            public string frameRate { get; set; }
            public List<fw_video_keyframe> keyframes { get; set; }

        }

        public class fw_video_keyframe
        {
            public string timestamp { get; set; }
            public List<fw_preview> images { get; set; }
        }

        #endregion

        #endregion
    }

}
