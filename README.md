# README #

## Build
1) Run `build.sh` with `Debug` or `Release` as arguments. E.g. `$ ./build.sh Release`
1) Pack the docker container: `docker build -f Dockerfile -t chaucer/chaucer:x.y .`, substituting x.y for some reasonable version number

### To do
* Consider expressing the entire build pipeline in the `Dockerfile` as explained [here](https://docs.docker.com/engine/examples/dotnetcore/).

## Roadmap
* Chaucer: https://composable.atlassian.net/projects/GEOF/issues

