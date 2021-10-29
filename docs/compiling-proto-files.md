# Compiling Proto Files

If you are using this library it comes with compiled proto files of Spotware Open API and we do our best to keep the files update, in case there was a new version of proto files available and we weren't updated the files in library you can clone the library and compile the new proto files, then replace the library proto files with your compiled ones, the message files are located at Protobuf project inside Messages directory.

For compiling the proto files there is a guide available on Spotware Open API documentation but that is out dated and if you compile the files by following their instruction you will endup with Protobuf 2.0 which is old version and not supported anymore by Google, the new Protobuf 3 compiler can compile the old version files, Open API uses 2.0 but you can use the new version compiler and benifit from all the new features of version 3.

If you use the old version compiled files then you can't use .NET Core, because the Google Protobuf 2 .NET library is only available for .NET framework.

We recommend you to use our compiling instruction instead of Spotware documentation instruction, this instruction is for Windows and you can follow the Google standard instruction on Protobuf documentation if you are using Linux.

* Download the proto files from Spotware provided link/repo
* Download the Google Protobuf latest version from [here](https://github.com/protocolbuffers/protobuf/releases)
* Extract the Google Protobuf, there will be a "bin" folder, copy the ".proto" files there
* Open "CMD", go to bin folder location, and type: 
```
protoc --csharp_out=. ./proto_file_name.proto
```
Instead of "proto_file_name.proto" you have to provide each of the proto files names, you have to execute this command for each proto file.

After executing the command there will be a ".cs" file for the proto file, you can use those files instead of library default message files.

Don't forget to update the library Google Protobuf Nuget package to the version that you used for compiling the proto files, otherwise you will see lots of errors and you will not be able to build the project.