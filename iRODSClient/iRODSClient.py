import sys
import os
import io

from irods.session import iRODSSession

#Create new irods session
def create_session(host, username, password, zone, port=1247):
     
     if type(host) is unicode:
         host = str(host)
     if type(username) is unicode:
         username = str(username)
     if type(password) is unicode:
         password = str(password)
     if type(zone) is unicode:
         zone = str(zone)

     session = iRODSSession(host=host, port = port, user = username , password = password, zone = zone)
    
     return session

#Get folder on this path
def get_collection(session, path):
    if type(path) is unicode:
        path = str(path)
    return session.collections.get(path)

def get_all_data_objects_recursively(collection):
    
    list = []

    if collection.data_objects.count > 0:
        for x in collection.data_objects:
            list.append(x.path)
        
    if collection.subcollections.count > 0:
       for col in collection.subcollections:
           list.extend(get_all_data_objects_recursively(col))

    return list


#Get subfolders on this path
def get_sub_collections(session , path):
    if type(path) is unicode:
        path = str(path)
    collection = session.collections.get(path)
    return collection.subcollections

#Get data objects in this folder
def get_data_objects(session,path):
    if type(path) is unicode:
        path = str(path)
    return session.collections.get(path).data_objects

#Get file in this patha
def get_data_object(session, path):
    if type(path) is unicode:
        path = str(path)
    return session.data_objects.get(path)

#Save file locally
def save_data_object_locally(session, path, local_path):
    
    if type(path) is unicode:
        path = str(path)
    if type(local_path) is unicode:
        local_path = str(local_path)

    print 'Downloading file ', path , ' locally to ', local_path 
    dataObject = session.data_objects.get(path)

    CHUNK = 1024 * 1024

    with io.open(local_path,'wb') as file:
        with dataObject.open('r') as idata:
           
            len = idata.seek(0,2) * 1.0
            idata.seek(0,0) 

            while True:
                chunk = idata.read(CHUNK)
                if not chunk:
                    print "Finished downloading " , round(len / 1024,0) , 'KB / ', round(len / 1024,0) ,'KB to', local_path
                    idata.close()
                    break
                d = file.seek(0,1) / 1024
                  
                print round(len / 1024,0) , 'KB / ', d ,'KB downloaded!                                    \r',
                file.write(chunk)
        idata.close()
    file.close()

    return 

def save_data_objects_locally(session, paths, local_paths):
    for i in range(len(paths)):
        print "\n"
        save_data_object_locally(session , paths[i] , local_paths[i])

    return

# Mark and Sammy please provide implemention
def calculate_probabilistic_inundation_raster(list_of_raster_filepaths, output_raster_file_path):
    return 

# Not sure how to connect to server and upload files.  Curtis will provide
# details here.
def export_rasters_to_geoserver():
    return ""

# Alternatively, rasters can be stored on irods so that Morteza can access it.
def export_rasters_to_iRODS(list_of_raster_files):
    return ""



