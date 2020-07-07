# README #

## Build Chaucer
### Quick build and test
This is a non-authoritative build. It does not clear out dangling files, nor does it produce docker containers.

1) Run `build.sh` with `Debug` or `Release` as arguments. E.g. `$ ./build.sh Release`

### Authoritative build and test
1) `cd Chaucer/`
1) `docker build -f Dockerfile -t chaucer/chaucer:latest` .

## Run Chaucer.Backend

1) Build the container using the authoritative build above
1) Run the container: `docker run -p 80:8080 chaucer/chaucer`
  * You can run the container headless by doing a `docker run -d -p 80:8080`
3) Open the swagger page: http://localhost:8080/swagger or view the health check at http://localhost:8080/health

### To do
* Consider expressing the entire build pipeline in the `Dockerfile` as explained [here](https://docs.docker.com/engine/examples/dotnetcore/).

## Roadmap
* Chaucer: https://composable.atlassian.net/projects/GEOF/issues

