"""
Script for the automatically uploading files to the geoserver
"""
# pip install tethys_dataset_services (if needed)
import os.path
import sys
import datetime
import zipfile
#from geoserver.catalog import Catalog
from tethys_dataset_services.engines import GeoServerSpatialDatasetEngine
#import random
import string
#import requests
#from requests.auth import HTTPBasicAuth

def assemble_url(self, endpoint,  *args):
        """
        Create a URL from all the args.
        """
        # endpoint = self.endpoint

        # Eliminate trailing slash if necessary
        print endpoint

        if endpoint[-1] == '/':
            endpoint = endpoint[:-1]

        pieces = list(args)
        pieces.insert(0, endpoint)

        print '/'.join(pieces)
        return '/'.join(pieces)


def upload_files_to_geoserver(filepaths, geoserver_uri, geoserver_rest_endpoint_url, workspace, username, password):

    if type(geoserver_uri) is unicode:
        geoserver_uri = str(geoserver_uri)

    if type(geoserver_rest_endpoint_url) is unicode:
        geoserver_rest_endpoint_url = str(geoserver_rest_endpoint_url)

    if type(workspace) is unicode:
        workspace = str(workspace)
            
    if type(username) is unicode:
        username = str(username)

    if type(password) is unicode:
        password = str(password)

   
    geoserver_engine = GeoServerSpatialDatasetEngine(endpoint=geoserver_rest_endpoint_url, 
                                                     username=username, password=password)
    #create workspace

    response = geoserver_engine.list_workspaces()

    if response['success']:
        workspaces = response['result']

    if workspace not in workspaces:
        print "I did something"
        response = geoserver_engine.create_workspace(workspace_id=workspace,uri=geoserver_uri)


    #upload to geoserver
    for filename in filepaths:

        store = filename.split("\\")[-1]
        store_id = workspace + ':' + store

        print 'uploading ' + filename + ' to store ' + store_id + '...'
       
        response = geoserver_engine.create_coverage_resource(store_id=store_id,
                                                              coverage_file = filename,
                                                              coverage_type='worldimage',
                                                              overwrite=True,
                                                              debug=True)
        
    return

def uploadStyle(file):
    												
	#create appropriate styling

	#response = geoserver_engine.list_styles()

	#sld_path = 'C:\\InundationForecasting\\ConfigurationFiles\\depth_raster.txt'

	#if response['success'] and 'depth_raster' not in response['result']:
		
	#	sld_string = ''
		
	#	with open(sld_path, 'r') as file:
	#		sld_string = file.read()
			
	#	response =
	#	geoserver_engine.create_style(style_id='first_responder:depth_raster',
	#											sld=sld_string,
	#											overwrite=True,
	#											debug=True,)

	#update layer engine.update_layer()

    return


#test = ["/Users/calebbuahin/Downloads/Shoal_Creek_1_20150701T1200Z.tif"]
#upload_files_to_geoserver(test , "http://apps.nfie.org/first-responder" , "http://nfie-team2.cloudapp.net:8181/geoserver/rest" , "first_responder", "admin" ,"geoserver");

