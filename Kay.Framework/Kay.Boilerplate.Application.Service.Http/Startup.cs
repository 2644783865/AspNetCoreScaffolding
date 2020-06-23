using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kay.Boilerplate.Infrastructure.BoundedContext.Ef;
using Kay.Framework.AspNetCore.Mvc.Attributes;
using Kay.Framework.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Kay.Framework.Extensions;
using Kay.Framework.AspNetCore.Mvc.EntityFrameworkCore;
using Kay.Framework.Domain.EntityFrameworkCore.DependencyInjection;
using Kay.Boilerplate.ApplicationService.IAppService;
using Kay.Boilerplate.Domain.Services;
using Kay.Framework.AspNetCore.Mvc.Containers;
using Kay.Framework.ObjectMapping.Abstractions;
using Kay.Framework.ObjectMapping.TinyMapper;
using Kay.Framework.AspNetCore.Exceptions;
using NLog.Web;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Kay.Framework.Redis;
using Kay.Boilerplate.Application.Service.Http.Filter;
using Kay.Framework.Swagger;

namespace Kay.Boilerplate.Application.Service.Http
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            #region NL-��֤
            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelAttribute>();
                options.Filters.Add<AuthorizationFilter>();
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            });

            //�ر�Ĭ���Զ�ValidateModel ��֤
            services.Configure<ApiBehaviorOptions>(opts => opts.SuppressModelStateInvalidFilter = true);

            #endregion ��֤

            #region Efʵ��ע��
            var dbType = Configuration.GetStringValue("DbType", "SqlServer");
            var dbConnection = Configuration.GetStringValue("DbConnectionString");

            services
                .AddDbContext<BoilerplateDbContext>(opt =>
                {
                    opt.UseNalongBuilder(dbType, dbConnection);
                })
                .AddDbContext<BoilerplateDbContext>()
                .AddEfUnitOfWork()
                .AddEfRepository();

            //Mysql��ע��
            //services.AddDbContext<WebBoilerplateMysqlDbContext>(opt =>
            //{
            //    opt.UseMySql(Configuration.GetStringValue("nalong.mysql"));
            //});

            #endregion Efʵ��ע��

            #region AppService��DomainService��Config��AutoMapper ע��
           
            services.AddAppService(typeof(IUserAppService).Assembly);
            services.AddDomainService(typeof(TbUserDomainService).Assembly);
            services.AddSingleton(typeof(IMapper), typeof(TinyMapperMapper));

            #endregion AppService��DomainService��Config��AutoMapper ע��;

            #region Redisע��
            //redis�����ַ���
            var redisConn = Configuration.GetSection("Redis").GetStringValue("ConnStr");
            services.AddSingleton(new RedisCliHelper(redisConn));
            #endregion

            services.AddSwaggerCustom(Configuration);

            services.AddSession();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            //����쳣�м��
            app.UseSession();
            app.UseException();

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseSwaggerCustom(Configuration);

            //����NLog ��־���ע��
            #region ��־
            loggerFactory.AddNLog();
            env.ConfigureNLog("NLog.config");
            #endregion

            app.UseMvc();
        }
    }
}
